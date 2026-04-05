using AtlasWeb.Data;
using AtlasWeb.DTOs;
using AtlasWeb.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace AtlasWeb.Services;

public sealed class HksBeldeService : IHksBeldeService
{
    private readonly AtlasDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IHksService _hksService;
    private readonly IHksIlceService _hksIlceService;

    public HksBeldeService(
        AtlasDbContext dbContext,
        ICurrentUserService currentUserService,
        IHksService hksService,
        IHksIlceService hksIlceService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _hksService = hksService;
        _hksIlceService = hksIlceService;
    }

    public async Task<IReadOnlyList<HksBeldeKayitDto>> GetCurrentTenantTownsAsync(int? ilceId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.HksBeldeler
            .IgnoreQueryFilters()
            .Where(x => x.AktifMi);

        if (ilceId.HasValue)
        {
            query = query.Where(x => x.HksIlceId == ilceId.Value);
        }

        return await query
            .OrderBy(x => x.Ad)
            .Select(x => ToDto(x))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<HksBeldeKayitDto>> SyncCurrentTenantTownsAsync(int? ilceId = null, CancellationToken cancellationToken = default)
    {
        _ = EnsureCurrentTenant();
        var districts = await EnsureDistrictsAsync(ilceId, cancellationToken);
        if (districts.Count == 0)
        {
            return [];
        }

        var remoteById = await BuildRemoteTownMapAsync(
            districts,
            (districtId, token) => _hksService.GetBeldelerAsync(districtId, token),
            cancellationToken);

        if (ilceId is null && remoteById.Count == 0)
        {
            throw new HksIntegrationException(
                "HKS belde listesi bos dondu. Kayitlar guncellenmedi.",
                StatusCodes.Status502BadGateway);
        }

        await PersistGlobalTownsAsync(
            remoteById,
            districts.Select(x => x.HksIlceId).ToHashSet(),
            fullRefresh: ilceId is null,
            cancellationToken);

        return await GetCurrentTenantTownsAsync(ilceId, cancellationToken);
    }

    public async Task<HksBeldeTopluSenkronSonucDto> SyncTownsForAllTenantsAsync(Guid sourceTenantId, CancellationToken cancellationToken = default)
    {
        if (sourceTenantId == Guid.Empty || sourceTenantId == AtlasDbContext.SystemMusteriId)
        {
            throw new HksIntegrationException(
                "Toplu HKS belde senkronu icin gecerli bir sirket secilmelidir.",
                StatusCodes.Status400BadRequest);
        }

        var districts = await EnsureSharedDistrictsAsync(sourceTenantId, cancellationToken);
        var remoteById = await BuildRemoteTownMapAsync(
            districts,
            (districtId, token) => _hksService.GetBeldelerForTenantAsync(sourceTenantId, districtId, token),
            cancellationToken);

        if (remoteById.Count == 0)
        {
            throw new HksIntegrationException(
                "HKS belde listesi bos dondu. Kayitlar guncellenmedi.",
                StatusCodes.Status502BadGateway);
        }

        await PersistGlobalTownsAsync(
            remoteById,
            districts.Select(x => x.HksIlceId).ToHashSet(),
            fullRefresh: true,
            cancellationToken);

        var tenantCount = await _dbContext.Musteriler
            .IgnoreQueryFilters()
            .CountAsync(x => x.AktifMi && x.Id != AtlasDbContext.SystemMusteriId, cancellationToken);

        return new HksBeldeTopluSenkronSonucDto
        {
            KaynakMusteriId = sourceTenantId,
            SirketSayisi = tenantCount,
            BeldeSayisi = remoteById.Count,
            GuncellemeTarihi = DateTime.UtcNow
        };
    }

    private Guid EnsureCurrentTenant()
    {
        if (_currentUserService.IsSystemAdmin || _currentUserService.MusteriId is null || _currentUserService.MusteriId == Guid.Empty)
        {
            throw new HksIntegrationException(
                "HKS belde listesi icin aktif sirket baglantisi gerekir.",
                StatusCodes.Status403Forbidden);
        }

        return _currentUserService.MusteriId.Value;
    }

    private async Task<List<HksIlceKayitDto>> EnsureDistrictsAsync(int? ilceId, CancellationToken cancellationToken)
    {
        var districts = await LoadSavedDistrictsAsync(ilceId, cancellationToken);
        if (districts.Count > 0)
        {
            return districts;
        }

        await _hksIlceService.SyncCurrentTenantDistrictsAsync(cancellationToken: cancellationToken);
        return await LoadSavedDistrictsAsync(ilceId, cancellationToken);
    }

    private async Task<List<HksIlceKayitDto>> EnsureSharedDistrictsAsync(Guid sourceTenantId, CancellationToken cancellationToken)
    {
        var districts = await LoadSavedDistrictsAsync(null, cancellationToken);
        if (districts.Count > 0)
        {
            return districts;
        }

        await _hksIlceService.SyncDistrictsForAllTenantsAsync(sourceTenantId, cancellationToken);
        return await LoadSavedDistrictsAsync(null, cancellationToken);
    }

    private async Task<List<HksIlceKayitDto>> LoadSavedDistrictsAsync(int? ilceId, CancellationToken cancellationToken)
    {
        var query = _dbContext.HksIlceler
            .IgnoreQueryFilters()
            .Where(x => x.AktifMi);

        if (ilceId.HasValue)
        {
            query = query.Where(x => x.HksIlceId == ilceId.Value);
        }

        return await query
            .OrderBy(x => x.HksIlceId)
            .Select(x => new HksIlceKayitDto
            {
                Id = x.Id,
                HksIlceId = x.HksIlceId,
                HksIlId = x.HksIlId,
                Ad = x.Ad,
                GuncellemeTarihi = x.GuncellemeTarihi ?? x.KayitTarihi
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<Dictionary<int, RemoteTownRecord>> BuildRemoteTownMapAsync(
        IReadOnlyList<HksIlceKayitDto> districts,
        Func<int, CancellationToken, Task<IReadOnlyList<HksSelectOptionDto>>> fetchTownsAsync,
        CancellationToken cancellationToken)
    {
        var remoteById = new Dictionary<int, RemoteTownRecord>();

        foreach (var district in districts)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var towns = await fetchTownsAsync(district.HksIlceId, cancellationToken);
            foreach (var town in towns.Where(item => item.Id > 0 && !string.IsNullOrWhiteSpace(item.Ad)))
            {
                remoteById[town.Id] = new RemoteTownRecord(town.Id, district.HksIlceId, town.Ad.Trim());
            }
        }

        return remoteById;
    }

    private async Task PersistGlobalTownsAsync(
        IReadOnlyDictionary<int, RemoteTownRecord> remoteById,
        IReadOnlySet<int> scopedDistrictIds,
        bool fullRefresh,
        CancellationToken cancellationToken)
    {
        var existingTowns = await _dbContext.HksBeldeler
            .IgnoreQueryFilters()
            .ToListAsync(cancellationToken);

        ApplyRemoteTowns(existingTowns, remoteById, scopedDistrictIds, fullRefresh);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private void ApplyRemoteTowns(
        List<HksBelde> existingTowns,
        IReadOnlyDictionary<int, RemoteTownRecord> remoteById,
        IReadOnlySet<int> scopedDistrictIds,
        bool fullRefresh)
    {
        var user = _currentUserService.EPosta ?? "System";

        foreach (var remoteTown in remoteById.Values)
        {
            var existing = existingTowns.FirstOrDefault(x => x.HksBeldeId == remoteTown.HksBeldeId);
            if (existing is null)
            {
                var entity = new HksBelde
                {
                    Id = IdGenerator.CreateV7(),
                    HksBeldeId = remoteTown.HksBeldeId,
                    HksIlceId = remoteTown.HksIlceId,
                    Ad = remoteTown.Ad,
                    AktifMi = true,
                    KayitTarihi = DateTime.UtcNow,
                    OlusturanKullanici = user,
                    Source = AuditSource.System
                };

                _dbContext.HksBeldeler.Add(entity);
                existingTowns.Add(entity);
                continue;
            }

            existing.HksIlceId = remoteTown.HksIlceId;
            existing.Ad = remoteTown.Ad;
            MarkUpdated(existing, user);
            Reactivate(existing);
        }

        var missingTowns = fullRefresh
            ? existingTowns.Where(x => !remoteById.ContainsKey(x.HksBeldeId))
            : existingTowns.Where(x => scopedDistrictIds.Contains(x.HksIlceId) && !remoteById.ContainsKey(x.HksBeldeId));

        foreach (var existing in missingTowns)
        {
            Deactivate(existing);
        }
    }

    private static void Reactivate(HksBelde entity)
    {
        if (entity.AktifMi)
        {
            return;
        }

        entity.AktifMi = true;
        entity.SilinmeTarihi = null;
        entity.SilenKullanici = null;
    }

    private void Deactivate(HksBelde entity)
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

    private static void MarkUpdated(HksBelde entity, string user)
    {
        entity.GuncellemeTarihi = DateTime.UtcNow;
        entity.GuncelleyenKullanici = user;
        entity.Source = AuditSource.System;
    }

    private static HksBeldeKayitDto ToDto(HksBelde entity)
    {
        return new HksBeldeKayitDto
        {
            Id = entity.Id,
            HksBeldeId = entity.HksBeldeId,
            HksIlceId = entity.HksIlceId,
            Ad = entity.Ad,
            GuncellemeTarihi = entity.GuncellemeTarihi ?? entity.KayitTarihi
        };
    }

    private sealed record RemoteTownRecord(int HksBeldeId, int HksIlceId, string Ad);
}
