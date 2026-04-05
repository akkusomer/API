using AtlasWeb.Data;
using AtlasWeb.DTOs;
using AtlasWeb.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace AtlasWeb.Services;

public sealed class HksIlService : IHksIlService
{
    private readonly AtlasDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IHksService _hksService;

    public HksIlService(
        AtlasDbContext dbContext,
        ICurrentUserService currentUserService,
        IHksService hksService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _hksService = hksService;
    }

    public async Task<IReadOnlyList<HksIlKayitDto>> GetCurrentTenantCitiesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.HksIller
            .IgnoreQueryFilters()
            .Where(x => x.AktifMi)
            .OrderBy(x => x.Ad)
            .Select(x => ToDto(x))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<HksIlKayitDto>> SyncCurrentTenantCitiesAsync(CancellationToken cancellationToken = default)
    {
        _ = EnsureCurrentTenant();
        var remoteById = BuildRemoteCityMap(await _hksService.GetIllerAsync(cancellationToken));
        await PersistGlobalCitiesAsync(remoteById, cancellationToken);
        return await GetCurrentTenantCitiesAsync(cancellationToken);
    }

    public async Task<HksIlTopluSenkronSonucDto> SyncCitiesForAllTenantsAsync(Guid sourceTenantId, CancellationToken cancellationToken = default)
    {
        if (sourceTenantId == Guid.Empty || sourceTenantId == AtlasDbContext.SystemMusteriId)
        {
            throw new HksIntegrationException(
                "Toplu HKS il senkronu icin gecerli bir sirket secilmelidir.",
                StatusCodes.Status400BadRequest);
        }

        var remoteById = BuildRemoteCityMap(await _hksService.GetIllerForTenantAsync(sourceTenantId, cancellationToken));
        return await SyncGlobalCitiesAsync(remoteById, includeTenantCount: true, sourceTenantId, cancellationToken);
    }

    private Guid EnsureCurrentTenant()
    {
        if (_currentUserService.IsSystemAdmin || _currentUserService.MusteriId is null || _currentUserService.MusteriId == Guid.Empty)
        {
            throw new HksIntegrationException(
                "HKS il listesi icin aktif sirket baglantisi gerekir.",
                StatusCodes.Status403Forbidden);
        }

        return _currentUserService.MusteriId.Value;
    }

    private static void Reactivate(HksIl entity)
    {
        if (entity.AktifMi)
        {
            return;
        }

        entity.AktifMi = true;
        entity.SilinmeTarihi = null;
        entity.SilenKullanici = null;
    }

    private async Task<HksIlTopluSenkronSonucDto> SyncGlobalCitiesAsync(
        IReadOnlyDictionary<int, HksSelectOptionDto> remoteById,
        bool includeTenantCount,
        Guid? sourceTenantId,
        CancellationToken cancellationToken)
    {
        await PersistGlobalCitiesAsync(remoteById, cancellationToken);

        var tenantCount = 0;
        if (includeTenantCount)
        {
            tenantCount = await _dbContext.Musteriler
                .IgnoreQueryFilters()
                .CountAsync(x => x.AktifMi && x.Id != AtlasDbContext.SystemMusteriId, cancellationToken);
        }

        return new HksIlTopluSenkronSonucDto
        {
            KaynakMusteriId = sourceTenantId ?? Guid.Empty,
            SirketSayisi = tenantCount,
            IlSayisi = remoteById.Count,
            GuncellemeTarihi = DateTime.UtcNow
        };
    }

    private async Task PersistGlobalCitiesAsync(
        IReadOnlyDictionary<int, HksSelectOptionDto> remoteById,
        CancellationToken cancellationToken)
    {
        var existingCities = await _dbContext.HksIller
            .IgnoreQueryFilters()
            .ToListAsync(cancellationToken);

        ApplyRemoteCities(existingCities, remoteById);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private void ApplyRemoteCities(
        List<HksIl> existingCities,
        IReadOnlyDictionary<int, HksSelectOptionDto> remoteById)
    {
        var user = _currentUserService.EPosta ?? "System";

        foreach (var remoteCity in remoteById.Values)
        {
            var existing = existingCities.FirstOrDefault(x => x.HksIlId == remoteCity.Id);
            if (existing is null)
            {
                var entity = new HksIl
                {
                    Id = IdGenerator.CreateV7(),
                    HksIlId = remoteCity.Id,
                    Ad = remoteCity.Ad.Trim(),
                    AktifMi = true,
                    KayitTarihi = DateTime.UtcNow,
                    OlusturanKullanici = user,
                    Source = AuditSource.System
                };

                _dbContext.HksIller.Add(entity);
                existingCities.Add(entity);
                continue;
            }

            existing.Ad = remoteCity.Ad.Trim();
            MarkUpdated(existing, user);
            Reactivate(existing);
        }

        foreach (var existing in existingCities.Where(x => !remoteById.ContainsKey(x.HksIlId)))
        {
            Deactivate(existing);
        }
    }

    private void Deactivate(HksIl entity)
    {
        if (!entity.AktifMi)
        {
            return;
        }

        entity.AktifMi = false;
        entity.SilinmeTarihi = DateTime.UtcNow;
        entity.SilenKullanici = _currentUserService.EPosta ?? "System";
        entity.GuncellemeTarihi = DateTime.UtcNow;
        entity.GuncelleyenKullanici = _currentUserService.EPosta ?? "System";
        entity.Source = AuditSource.System;
    }

    private static void MarkUpdated(HksIl entity, string user)
    {
        entity.GuncellemeTarihi = DateTime.UtcNow;
        entity.GuncelleyenKullanici = user;
        entity.Source = AuditSource.System;
    }

    private static IReadOnlyDictionary<int, HksSelectOptionDto> BuildRemoteCityMap(IReadOnlyList<HksSelectOptionDto> remoteCities)
    {
        var remoteById = remoteCities
            .Where(item => item.Id > 0 && !string.IsNullOrWhiteSpace(item.Ad))
            .GroupBy(item => item.Id)
            .Select(group => group.First())
            .ToDictionary(item => item.Id);

        if (remoteById.Count > 0)
        {
            return remoteById;
        }

        throw new HksIntegrationException(
            "HKS il listesi bos dondu. Kayitlar guncellenmedi.",
            StatusCodes.Status502BadGateway);
    }

    private static HksIlKayitDto ToDto(HksIl entity)
    {
        return new HksIlKayitDto
        {
            Id = entity.Id,
            HksIlId = entity.HksIlId,
            Ad = entity.Ad,
            GuncellemeTarihi = entity.GuncellemeTarihi ?? entity.KayitTarihi
        };
    }
}
