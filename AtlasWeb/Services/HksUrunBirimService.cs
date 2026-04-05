using AtlasWeb.Data;
using AtlasWeb.DTOs;
using AtlasWeb.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace AtlasWeb.Services;

public sealed class HksUrunBirimService : IHksUrunBirimService
{
    private readonly AtlasDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IHksService _hksService;

    public HksUrunBirimService(
        AtlasDbContext dbContext,
        ICurrentUserService currentUserService,
        IHksService hksService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _hksService = hksService;
    }

    public async Task<IReadOnlyList<HksUrunBirimKayitDto>> GetCurrentTenantProductUnitsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.HksUrunBirimleri
            .IgnoreQueryFilters()
            .Where(x => x.AktifMi)
            .OrderBy(x => x.Ad)
            .Select(x => ToDto(x))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<HksUrunBirimKayitDto>> SyncCurrentTenantProductUnitsAsync(CancellationToken cancellationToken = default)
    {
        _ = EnsureCurrentTenant();
        var remoteById = BuildRemoteMap(await _hksService.GetUrunBirimleriAsync(cancellationToken));
        await PersistGlobalProductUnitsAsync(remoteById, cancellationToken);
        return await GetCurrentTenantProductUnitsAsync(cancellationToken);
    }

    private Guid EnsureCurrentTenant()
    {
        if (_currentUserService.IsSystemAdmin || _currentUserService.MusteriId is null || _currentUserService.MusteriId == Guid.Empty)
        {
            throw new HksIntegrationException(
                "HKS urun birimi listesi icin aktif sirket baglantisi gerekir.",
                StatusCodes.Status403Forbidden);
        }

        return _currentUserService.MusteriId.Value;
    }

    private async Task PersistGlobalProductUnitsAsync(
        IReadOnlyDictionary<int, HksSelectOptionDto> remoteById,
        CancellationToken cancellationToken)
    {
        var existingUnits = await _dbContext.HksUrunBirimleri
            .IgnoreQueryFilters()
            .ToListAsync(cancellationToken);

        ApplyRemoteProductUnits(existingUnits, remoteById);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private void ApplyRemoteProductUnits(
        List<HksUrunBirim> existingUnits,
        IReadOnlyDictionary<int, HksSelectOptionDto> remoteById)
    {
        var user = _currentUserService.EPosta ?? "System";

        foreach (var remoteUnit in remoteById.Values)
        {
            var existing = existingUnits.FirstOrDefault(x => x.HksUrunBirimId == remoteUnit.Id);
            if (existing is null)
            {
                var entity = new HksUrunBirim
                {
                    Id = IdGenerator.CreateV7(),
                    HksUrunBirimId = remoteUnit.Id,
                    Ad = remoteUnit.Ad.Trim(),
                    AktifMi = true,
                    KayitTarihi = DateTime.UtcNow,
                    OlusturanKullanici = user,
                    Source = AuditSource.System
                };

                _dbContext.HksUrunBirimleri.Add(entity);
                existingUnits.Add(entity);
                continue;
            }

            existing.Ad = remoteUnit.Ad.Trim();
            MarkUpdated(existing, user);
            Reactivate(existing);
        }

        foreach (var existing in existingUnits.Where(x => !remoteById.ContainsKey(x.HksUrunBirimId)))
        {
            Deactivate(existing);
        }
    }

    private static void Reactivate(HksUrunBirim entity)
    {
        if (entity.AktifMi)
        {
            return;
        }

        entity.AktifMi = true;
        entity.SilinmeTarihi = null;
        entity.SilenKullanici = null;
    }

    private void Deactivate(HksUrunBirim entity)
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

    private static void MarkUpdated(HksUrunBirim entity, string user)
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
            "HKS urun birimi listesi bos dondu. Kayitlar guncellenmedi.",
            StatusCodes.Status502BadGateway);
    }

    private static HksUrunBirimKayitDto ToDto(HksUrunBirim entity)
    {
        return new HksUrunBirimKayitDto
        {
            Id = entity.Id,
            HksUrunBirimId = entity.HksUrunBirimId,
            Ad = entity.Ad,
            GuncellemeTarihi = entity.GuncellemeTarihi ?? entity.KayitTarihi
        };
    }
}
