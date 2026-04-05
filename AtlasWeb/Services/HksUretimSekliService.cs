using AtlasWeb.Data;
using AtlasWeb.DTOs;
using AtlasWeb.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace AtlasWeb.Services;

public sealed class HksUretimSekliService : IHksUretimSekliService
{
    private readonly AtlasDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IHksService _hksService;

    public HksUretimSekliService(
        AtlasDbContext dbContext,
        ICurrentUserService currentUserService,
        IHksService hksService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _hksService = hksService;
    }

    public async Task<IReadOnlyList<HksUretimSekliKayitDto>> GetCurrentTenantProductionShapesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.HksUretimSekilleri
            .IgnoreQueryFilters()
            .Where(x => x.AktifMi)
            .OrderBy(x => x.Ad)
            .Select(x => ToDto(x))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<HksUretimSekliKayitDto>> SyncCurrentTenantProductionShapesAsync(CancellationToken cancellationToken = default)
    {
        _ = EnsureCurrentTenant();
        var remoteById = BuildRemoteMap(await _hksService.GetUretimSekilleriAsync(cancellationToken));
        await PersistGlobalProductionShapesAsync(remoteById, cancellationToken);
        return await GetCurrentTenantProductionShapesAsync(cancellationToken);
    }

    private Guid EnsureCurrentTenant()
    {
        if (_currentUserService.IsSystemAdmin || _currentUserService.MusteriId is null || _currentUserService.MusteriId == Guid.Empty)
        {
            throw new HksIntegrationException(
                "HKS uretim sekli listesi icin aktif sirket baglantisi gerekir.",
                StatusCodes.Status403Forbidden);
        }

        return _currentUserService.MusteriId.Value;
    }

    private async Task PersistGlobalProductionShapesAsync(
        IReadOnlyDictionary<int, HksSelectOptionDto> remoteById,
        CancellationToken cancellationToken)
    {
        var existingShapes = await _dbContext.HksUretimSekilleri
            .IgnoreQueryFilters()
            .ToListAsync(cancellationToken);

        ApplyRemoteProductionShapes(existingShapes, remoteById);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private void ApplyRemoteProductionShapes(
        List<HksUretimSekli> existingShapes,
        IReadOnlyDictionary<int, HksSelectOptionDto> remoteById)
    {
        var user = _currentUserService.EPosta ?? "System";

        foreach (var remoteShape in remoteById.Values)
        {
            var existing = existingShapes.FirstOrDefault(x => x.HksUretimSekliId == remoteShape.Id);
            if (existing is null)
            {
                var entity = new HksUretimSekli
                {
                    Id = IdGenerator.CreateV7(),
                    HksUretimSekliId = remoteShape.Id,
                    Ad = remoteShape.Ad.Trim(),
                    AktifMi = true,
                    KayitTarihi = DateTime.UtcNow,
                    OlusturanKullanici = user,
                    Source = AuditSource.System
                };

                _dbContext.HksUretimSekilleri.Add(entity);
                existingShapes.Add(entity);
                continue;
            }

            existing.Ad = remoteShape.Ad.Trim();
            MarkUpdated(existing, user);
            Reactivate(existing);
        }

        foreach (var existing in existingShapes.Where(x => !remoteById.ContainsKey(x.HksUretimSekliId)))
        {
            Deactivate(existing);
        }
    }

    private static void Reactivate(HksUretimSekli entity)
    {
        if (entity.AktifMi)
        {
            return;
        }

        entity.AktifMi = true;
        entity.SilinmeTarihi = null;
        entity.SilenKullanici = null;
    }

    private void Deactivate(HksUretimSekli entity)
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

    private static void MarkUpdated(HksUretimSekli entity, string user)
    {
        entity.GuncellemeTarihi = DateTime.UtcNow;
        entity.GuncelleyenKullanici = user;
        entity.Source = AuditSource.System;
    }

    private static IReadOnlyDictionary<int, HksSelectOptionDto> BuildRemoteMap(IReadOnlyList<HksSelectOptionDto> remoteItems)
    {
        var remoteById = remoteItems
            .Where(item => item.Id > 0 && !string.IsNullOrWhiteSpace(item.Ad))
            .GroupBy(item => item.Id)
            .Select(group => group.First())
            .ToDictionary(item => item.Id);

        if (remoteById.Count > 0)
        {
            return remoteById;
        }

        throw new HksIntegrationException(
            "HKS uretim sekli listesi bos dondu. Kayitlar guncellenmedi.",
            StatusCodes.Status502BadGateway);
    }

    private static HksUretimSekliKayitDto ToDto(HksUretimSekli entity)
    {
        return new HksUretimSekliKayitDto
        {
            Id = entity.Id,
            HksUretimSekliId = entity.HksUretimSekliId,
            Ad = entity.Ad,
            GuncellemeTarihi = entity.GuncellemeTarihi ?? entity.KayitTarihi
        };
    }
}
