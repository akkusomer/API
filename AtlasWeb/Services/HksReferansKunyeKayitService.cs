using System.Text.Json;
using AtlasWeb.Data;
using AtlasWeb.DTOs;
using AtlasWeb.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace AtlasWeb.Services;

public sealed class HksReferansKunyeKayitService : IHksReferansKunyeKayitService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly AtlasDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<HksReferansKunyeKayitService> _logger;

    public HksReferansKunyeKayitService(
        AtlasDbContext dbContext,
        ICurrentUserService currentUserService,
        ILogger<HksReferansKunyeKayitService> logger)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<HksReferansKunyeKayitDto?> GetCurrentTenantSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = EnsureTenant();
        var entity = await _dbContext.HksReferansKunyeKayitlari
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.MusteriId == tenantId, cancellationToken);

        return entity is null ? null : ToDto(entity);
    }

    public async Task<HksReferansKunyeKayitDto> QueueCurrentTenantSearchAsync(
        HksReferansKunyeRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = EnsureTenant();
        var isNew = false;
        var entity = await _dbContext.HksReferansKunyeKayitlari
            .FirstOrDefaultAsync(x => x.MusteriId == tenantId, cancellationToken);

        if (entity is not null
            && (string.Equals(entity.Durum, HksReferansKunyeDurum.Kuyrukta, StringComparison.OrdinalIgnoreCase)
                || string.Equals(entity.Durum, HksReferansKunyeDurum.Isleniyor, StringComparison.OrdinalIgnoreCase)))
        {
            throw new HksIntegrationException(
                "Zaten devam eden bir HKS sorgusu var.",
                StatusCodes.Status409Conflict);
        }

        if (entity is null)
        {
            entity = new HksReferansKunyeKayit();
            isNew = true;
        }

        entity.BaslangicTarihi = request.BaslangicTarihi;
        entity.BitisTarihi = request.BitisTarihi;
        entity.Durum = HksReferansKunyeDurum.Kuyrukta;
        entity.ProgressPercent = 0;
        entity.ProgressLabel = "HKS sorgusu kuyruga alindi";
        entity.Hata = null;
        entity.IslemKodu = null;
        entity.Mesaj = null;
        entity.KayitSayisi = 0;
        entity.ReferansKunyelerJson = "[]";

        if (isNew)
        {
            _dbContext.HksReferansKunyeKayitlari.Add(entity);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<HksReferansKunyeKayitDto> SaveCurrentTenantSnapshotAsync(
        HksReferansKunyeKaydetDto request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = EnsureTenant();
        var isNew = false;
        var entity = await _dbContext.HksReferansKunyeKayitlari
            .FirstOrDefaultAsync(x => x.MusteriId == tenantId, cancellationToken);

        if (entity is null)
        {
            entity = new HksReferansKunyeKayit();
            isNew = true;
        }

        entity.BaslangicTarihi = request.BaslangicTarihi;
        entity.BitisTarihi = request.BitisTarihi;
        entity.IslemKodu = Normalize(request.IslemKodu);
        entity.Mesaj = Normalize(request.Mesaj);

        if (request.ReferansKunyeler is not null)
        {
            entity.KayitSayisi = request.ReferansKunyeler.Count;
            entity.ReferansKunyelerJson = JsonSerializer.Serialize(request.ReferansKunyeler, JsonOptions);
        }
        else if (isNew)
        {
            entity.KayitSayisi = 0;
            entity.ReferansKunyelerJson = "[]";
        }

        if (isNew)
        {
            _dbContext.HksReferansKunyeKayitlari.Add(entity);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    private Guid EnsureTenant()
    {
        if (_currentUserService.IsSystemAdmin || _currentUserService.MusteriId is null || _currentUserService.MusteriId == Guid.Empty)
        {
            throw new HksIntegrationException(
                "HKS kayitlari icin aktif sirket baglantisi gerekir.",
                StatusCodes.Status403Forbidden);
        }

        return _currentUserService.MusteriId.Value;
    }

    private HksReferansKunyeKayitDto ToDto(HksReferansKunyeKayit entity)
    {
        return new HksReferansKunyeKayitDto
        {
            Durum = string.IsNullOrWhiteSpace(entity.Durum) ? HksReferansKunyeDurum.Bos : entity.Durum,
            ProgressPercent = entity.ProgressPercent,
            ProgressLabel = entity.ProgressLabel,
            Hata = entity.Hata,
            BaslangicTarihi = entity.BaslangicTarihi,
            BitisTarihi = entity.BitisTarihi,
            IslemKodu = entity.IslemKodu,
            Mesaj = entity.Mesaj,
            KayitSayisi = entity.KayitSayisi,
            GuncellemeTarihi = entity.GuncellemeTarihi ?? entity.KayitTarihi,
            ReferansKunyeler = DeserializeItems(entity.ReferansKunyelerJson),
        };
    }

    private List<HksReferansKunyeDto> DeserializeItems(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<HksReferansKunyeDto>>(json, JsonOptions) ?? [];
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "HKS referans kunye kayit json verisi parse edilemedi.");
            return [];
        }
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
