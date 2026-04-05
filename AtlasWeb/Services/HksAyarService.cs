using AtlasWeb.Data;
using AtlasWeb.DTOs;
using AtlasWeb.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace AtlasWeb.Services;

public sealed class HksAyarService : IHksAyarService
{
    private const string ApplicationName = "AtlasWeb";
    private const string ProtectorPurpose = "AtlasWeb.HksTenantSettings";

    private readonly AtlasDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDataProtector _protector;
    private readonly IReadOnlyList<IDataProtector> _legacyProtectors;
    private readonly ILogger<HksAyarService> _logger;

    public HksAyarService(
        AtlasDbContext dbContext,
        ICurrentUserService currentUserService,
        IDataProtectionProvider dataProtectionProvider,
        IHostEnvironment hostEnvironment,
        ILogger<HksAyarService> logger)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _protector = dataProtectionProvider.CreateProtector(ProtectorPurpose);
        _legacyProtectors = BuildLegacyProtectors(hostEnvironment);
        _logger = logger;
    }

    public async Task<HksAyarDto> GetCurrentTenantSettingsAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = EnsureCurrentTenant();
        return await GetTenantSettingsAsync(tenantId, cancellationToken);
    }

    public async Task<HksAyarDto> GetTenantSettingsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.HksAyarlari
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.MusteriId == tenantId, cancellationToken);

        return ToDto(entity);
    }

    public async Task<HksAyarDto> SaveCurrentTenantSettingsAsync(
        HksAyarKaydetDto request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = EnsureCurrentTenant();
        var entity = await _dbContext.HksAyarlari
            .FirstOrDefaultAsync(x => x.MusteriId == tenantId, cancellationToken);

        var isNew = entity is null;
        entity ??= new HksAyar();

        var kullaniciAdi = Normalize(request.KullaniciAdi);
        if (string.IsNullOrWhiteSpace(kullaniciAdi))
        {
            throw new ArgumentException("HKS kullanici adi zorunludur.");
        }

        entity.KullaniciAdi = kullaniciAdi;

        var password = Normalize(request.Password);
        if (!string.IsNullOrWhiteSpace(password))
        {
            entity.PasswordCipherText = _protector.Protect(password);
        }

        var servicePassword = Normalize(request.ServicePassword);
        if (!string.IsNullOrWhiteSpace(servicePassword))
        {
            entity.ServicePasswordCipherText = _protector.Protect(servicePassword);
        }

        if (isNew)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(servicePassword))
            {
                throw new ArgumentException("Ilk kayitta HKS sifresi ve servis sifresi zorunludur.");
            }

            _dbContext.HksAyarlari.Add(entity);
        }
        else if (string.IsNullOrWhiteSpace(entity.PasswordCipherText) || string.IsNullOrWhiteSpace(entity.ServicePasswordCipherText))
        {
            throw new ArgumentException("Kayitli HKS sifreleri eksik. Lutfen iki sifreyi de yeniden girin.");
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(entity);
    }

    public async Task<HksCredentialSet> GetCurrentTenantCredentialsAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = EnsureCurrentTenant();
        return await GetTenantCredentialsAsync(tenantId, cancellationToken);
    }

    public async Task<HksCredentialSet> GetTenantCredentialsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.HksAyarlari
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.MusteriId == tenantId, cancellationToken);

        if (entity is null
            || string.IsNullOrWhiteSpace(entity.KullaniciAdi)
            || string.IsNullOrWhiteSpace(entity.PasswordCipherText)
            || string.IsNullOrWhiteSpace(entity.ServicePasswordCipherText))
        {
            throw new HksIntegrationException(
                "Sirket icin HKS ayarlari kaydedilmemis. Once Ayarlar ekranindan HKS kullanici bilgilerini girin.",
                StatusCodes.Status503ServiceUnavailable);
        }

        if (!TryUnprotect(entity.PasswordCipherText, out var password, out var passwordUsedLegacy)
            || !TryUnprotect(entity.ServicePasswordCipherText, out var servicePassword, out var servicePasswordUsedLegacy))
        {
            throw new HksIntegrationException(
                "Sirket icin kayitli HKS sifreleri okunamadi. Ayarlar ekranindan tekrar kaydedin.",
                StatusCodes.Status503ServiceUnavailable);
        }

        if (passwordUsedLegacy || servicePasswordUsedLegacy)
        {
            entity.PasswordCipherText = _protector.Protect(password);
            entity.ServicePasswordCipherText = _protector.Protect(servicePassword);
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Tenant HKS credentials were re-protected with the stable application name for tenant {TenantId}.", tenantId);
        }

        return new HksCredentialSet
        {
            UserName = entity.KullaniciAdi,
            Password = password,
            ServicePassword = servicePassword,
        };
    }

    private Guid EnsureCurrentTenant()
    {
        if (_currentUserService.IsSystemAdmin || _currentUserService.MusteriId is null || _currentUserService.MusteriId == Guid.Empty)
        {
            throw new HksIntegrationException(
                "HKS ayarlari icin aktif sirket baglantisi gerekir.",
                StatusCodes.Status403Forbidden);
        }

        return _currentUserService.MusteriId.Value;
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private bool TryUnprotect(string cipherText, out string value, out bool usedLegacy)
    {
        usedLegacy = false;

        try
        {
            value = _protector.Unprotect(cipherText);
            return true;
        }
        catch
        {
            foreach (var protector in _legacyProtectors)
            {
                try
                {
                    value = protector.Unprotect(cipherText);
                    usedLegacy = true;
                    return true;
                }
                catch
                {
                    // Try next legacy application discriminator.
                }
            }

            value = string.Empty;
            return false;
        }
    }

    private static IReadOnlyList<IDataProtector> BuildLegacyProtectors(IHostEnvironment hostEnvironment)
    {
        var protectors = new List<IDataProtector>();
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrWhiteSpace(userProfile))
        {
            return protectors;
        }

        var keyDirectory = Path.Combine(userProfile, ".aspnet", "DataProtection-Keys");
        if (!Directory.Exists(keyDirectory))
        {
            return protectors;
        }

        var contentRoot = hostEnvironment.ContentRootPath;
        if (string.IsNullOrWhiteSpace(contentRoot))
        {
            return protectors;
        }

        var releasesDirectory = Directory.GetParent(contentRoot)?.FullName;
        if (string.IsNullOrWhiteSpace(releasesDirectory) || !Directory.Exists(releasesDirectory))
        {
            return protectors;
        }

        foreach (var releasePath in Directory.EnumerateDirectories(releasesDirectory))
        {
            foreach (var discriminator in GetLegacyApplicationNames(releasePath, contentRoot))
            {
                var provider = DataProtectionProvider.Create(
                    new DirectoryInfo(keyDirectory),
                    options => options.SetApplicationName(discriminator));

                protectors.Add(provider.CreateProtector(ProtectorPurpose));
            }
        }

        return protectors;
    }

    private static IEnumerable<string> GetLegacyApplicationNames(string releasePath, string currentContentRoot)
    {
        if (string.Equals(releasePath, currentContentRoot, StringComparison.OrdinalIgnoreCase))
        {
            yield break;
        }

        var normalized = Path.GetFullPath(releasePath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            yield break;
        }

        yield return normalized;
        yield return normalized + Path.DirectorySeparatorChar;
    }

    private static HksAyarDto ToDto(HksAyar? entity)
    {
        return new HksAyarDto
        {
            KullaniciAdi = entity?.KullaniciAdi,
            HasPassword = !string.IsNullOrWhiteSpace(entity?.PasswordCipherText),
            HasServicePassword = !string.IsNullOrWhiteSpace(entity?.ServicePasswordCipherText),
            GuncellemeTarihi = entity?.GuncellemeTarihi ?? entity?.KayitTarihi,
        };
    }
}
