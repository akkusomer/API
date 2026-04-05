using AtlasWeb.Data;
using AtlasWeb.DTOs;
using AtlasWeb.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace AtlasWeb.Services;

public sealed class HksIlceService : IHksIlceService
{
    private readonly AtlasDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IHksService _hksService;
    private readonly IHksIlService _hksIlService;

    public HksIlceService(
        AtlasDbContext dbContext,
        ICurrentUserService currentUserService,
        IHksService hksService,
        IHksIlService hksIlService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _hksService = hksService;
        _hksIlService = hksIlService;
    }

    public async Task<IReadOnlyList<HksIlceKayitDto>> GetCurrentTenantDistrictsAsync(int? ilId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.HksIlceler
            .IgnoreQueryFilters()
            .Where(x => x.AktifMi);

        if (ilId.HasValue)
        {
            query = query.Where(x => x.HksIlId == ilId.Value);
        }

        return await query
            .OrderBy(x => x.Ad)
            .Select(x => ToDto(x))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<HksIlceKayitDto>> SyncCurrentTenantDistrictsAsync(int? ilId = null, CancellationToken cancellationToken = default)
    {
        _ = EnsureCurrentTenant();
        var cities = await EnsureCitiesAsync(ilId, cancellationToken);
        if (cities.Count == 0)
        {
            return [];
        }

        var remoteById = await BuildRemoteDistrictMapAsync(
            cities,
            (cityId, token) => _hksService.GetIlcelerAsync(cityId, token),
            cancellationToken);

        if (ilId is null && remoteById.Count == 0)
        {
            throw new HksIntegrationException(
                "HKS ilce listesi bos dondu. Kayitlar guncellenmedi.",
                StatusCodes.Status502BadGateway);
        }

        await PersistGlobalDistrictsAsync(
            remoteById,
            cities.Select(x => x.HksIlId).ToHashSet(),
            fullRefresh: ilId is null,
            cancellationToken);

        return await GetCurrentTenantDistrictsAsync(ilId, cancellationToken);
    }

    public async Task<HksIlceTopluSenkronSonucDto> SyncDistrictsForAllTenantsAsync(Guid sourceTenantId, CancellationToken cancellationToken = default)
    {
        if (sourceTenantId == Guid.Empty || sourceTenantId == AtlasDbContext.SystemMusteriId)
        {
            throw new HksIntegrationException(
                "Toplu HKS ilce senkronu icin gecerli bir sirket secilmelidir.",
                StatusCodes.Status400BadRequest);
        }

        var cities = await EnsureSharedCitiesAsync(sourceTenantId, cancellationToken);
        var remoteById = await BuildRemoteDistrictMapAsync(
            cities,
            (cityId, token) => _hksService.GetIlcelerForTenantAsync(sourceTenantId, cityId, token),
            cancellationToken);

        if (remoteById.Count == 0)
        {
            throw new HksIntegrationException(
                "HKS ilce listesi bos dondu. Kayitlar guncellenmedi.",
                StatusCodes.Status502BadGateway);
        }

        await PersistGlobalDistrictsAsync(
            remoteById,
            cities.Select(x => x.HksIlId).ToHashSet(),
            fullRefresh: true,
            cancellationToken);

        var tenantCount = await _dbContext.Musteriler
            .IgnoreQueryFilters()
            .CountAsync(x => x.AktifMi && x.Id != AtlasDbContext.SystemMusteriId, cancellationToken);

        return new HksIlceTopluSenkronSonucDto
        {
            KaynakMusteriId = sourceTenantId,
            SirketSayisi = tenantCount,
            IlceSayisi = remoteById.Count,
            GuncellemeTarihi = DateTime.UtcNow
        };
    }

    private Guid EnsureCurrentTenant()
    {
        if (_currentUserService.IsSystemAdmin || _currentUserService.MusteriId is null || _currentUserService.MusteriId == Guid.Empty)
        {
            throw new HksIntegrationException(
                "HKS ilce listesi icin aktif sirket baglantisi gerekir.",
                StatusCodes.Status403Forbidden);
        }

        return _currentUserService.MusteriId.Value;
    }

    private async Task<List<HksIlKayitDto>> EnsureCitiesAsync(int? ilId, CancellationToken cancellationToken)
    {
        var cities = await LoadSavedCitiesAsync(ilId, cancellationToken);
        if (cities.Count > 0)
        {
            return cities;
        }

        await _hksIlService.SyncCurrentTenantCitiesAsync(cancellationToken);
        return await LoadSavedCitiesAsync(ilId, cancellationToken);
    }

    private async Task<List<HksIlKayitDto>> EnsureSharedCitiesAsync(Guid sourceTenantId, CancellationToken cancellationToken)
    {
        var cities = await LoadSavedCitiesAsync(null, cancellationToken);
        if (cities.Count > 0)
        {
            return cities;
        }

        await _hksIlService.SyncCitiesForAllTenantsAsync(sourceTenantId, cancellationToken);
        return await LoadSavedCitiesAsync(null, cancellationToken);
    }

    private async Task<List<HksIlKayitDto>> LoadSavedCitiesAsync(int? ilId, CancellationToken cancellationToken)
    {
        var query = _dbContext.HksIller
            .IgnoreQueryFilters()
            .Where(x => x.AktifMi);

        if (ilId.HasValue)
        {
            query = query.Where(x => x.HksIlId == ilId.Value);
        }

        return await query
            .OrderBy(x => x.HksIlId)
            .Select(x => new HksIlKayitDto
            {
                Id = x.Id,
                HksIlId = x.HksIlId,
                Ad = x.Ad,
                GuncellemeTarihi = x.GuncellemeTarihi ?? x.KayitTarihi
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<Dictionary<int, RemoteDistrictRecord>> BuildRemoteDistrictMapAsync(
        IReadOnlyList<HksIlKayitDto> cities,
        Func<int, CancellationToken, Task<IReadOnlyList<HksSelectOptionDto>>> fetchDistrictsAsync,
        CancellationToken cancellationToken)
    {
        var remoteById = new Dictionary<int, RemoteDistrictRecord>();

        foreach (var city in cities)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var districts = await fetchDistrictsAsync(city.HksIlId, cancellationToken);
            foreach (var district in districts.Where(item => item.Id > 0 && !string.IsNullOrWhiteSpace(item.Ad)))
            {
                remoteById[district.Id] = new RemoteDistrictRecord(district.Id, city.HksIlId, district.Ad.Trim());
            }
        }

        return remoteById;
    }

    private async Task PersistGlobalDistrictsAsync(
        IReadOnlyDictionary<int, RemoteDistrictRecord> remoteById,
        IReadOnlySet<int> scopedCityIds,
        bool fullRefresh,
        CancellationToken cancellationToken)
    {
        var existingDistricts = await _dbContext.HksIlceler
            .IgnoreQueryFilters()
            .ToListAsync(cancellationToken);

        ApplyRemoteDistricts(existingDistricts, remoteById, scopedCityIds, fullRefresh);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private void ApplyRemoteDistricts(
        List<HksIlce> existingDistricts,
        IReadOnlyDictionary<int, RemoteDistrictRecord> remoteById,
        IReadOnlySet<int> scopedCityIds,
        bool fullRefresh)
    {
        var user = _currentUserService.EPosta ?? "System";

        foreach (var remoteDistrict in remoteById.Values)
        {
            var existing = existingDistricts.FirstOrDefault(x => x.HksIlceId == remoteDistrict.HksIlceId);
            if (existing is null)
            {
                var entity = new HksIlce
                {
                    Id = IdGenerator.CreateV7(),
                    HksIlceId = remoteDistrict.HksIlceId,
                    HksIlId = remoteDistrict.HksIlId,
                    Ad = remoteDistrict.Ad,
                    AktifMi = true,
                    KayitTarihi = DateTime.UtcNow,
                    OlusturanKullanici = user,
                    Source = AuditSource.System
                };

                _dbContext.HksIlceler.Add(entity);
                existingDistricts.Add(entity);
                continue;
            }

            existing.HksIlId = remoteDistrict.HksIlId;
            existing.Ad = remoteDistrict.Ad;
            MarkUpdated(existing, user);
            Reactivate(existing);
        }

        var missingDistricts = fullRefresh
            ? existingDistricts.Where(x => !remoteById.ContainsKey(x.HksIlceId))
            : existingDistricts.Where(x => scopedCityIds.Contains(x.HksIlId) && !remoteById.ContainsKey(x.HksIlceId));

        foreach (var existing in missingDistricts)
        {
            Deactivate(existing);
        }
    }

    private static void Reactivate(HksIlce entity)
    {
        if (entity.AktifMi)
        {
            return;
        }

        entity.AktifMi = true;
        entity.SilinmeTarihi = null;
        entity.SilenKullanici = null;
    }

    private void Deactivate(HksIlce entity)
    {
        if (!entity.AktifMi)
        {
            return;
        }

        var user = _currentUserService.EPosta ?? "System";
        entity.AktifMi = false;
        entity.SilinmeTarihi = DateTime.UtcNow;
        entity.SilenKullanici = user;
        entity.GuncellemeTarihi = DateTime.UtcNow;
        entity.GuncelleyenKullanici = user;
        entity.Source = AuditSource.System;
    }

    private static void MarkUpdated(HksIlce entity, string user)
    {
        entity.GuncellemeTarihi = DateTime.UtcNow;
        entity.GuncelleyenKullanici = user;
        entity.Source = AuditSource.System;
    }

    private static HksIlceKayitDto ToDto(HksIlce entity)
    {
        return new HksIlceKayitDto
        {
            Id = entity.Id,
            HksIlceId = entity.HksIlceId,
            HksIlId = entity.HksIlId,
            Ad = entity.Ad,
            GuncellemeTarihi = entity.GuncellemeTarihi ?? entity.KayitTarihi
        };
    }

    private sealed record RemoteDistrictRecord(int HksIlceId, int HksIlId, string Ad);
}
