using AtlasWeb.Data;
using AtlasWeb.DTOs;
using AtlasWeb.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace AtlasWeb.Services;

public sealed class HksUrunService : IHksUrunService
{
    private readonly AtlasDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IHksService _hksService;

    public HksUrunService(
        AtlasDbContext dbContext,
        ICurrentUserService currentUserService,
        IHksService hksService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _hksService = hksService;
    }

    public async Task<IReadOnlyList<HksUrunKayitDto>> GetCurrentTenantProductsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.HksUrunler
            .IgnoreQueryFilters()
            .Where(x => x.AktifMi)
            .OrderBy(x => x.Ad)
            .Select(x => ToDto(x))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<HksUrunKayitDto>> SyncCurrentTenantProductsAsync(CancellationToken cancellationToken = default)
    {
        _ = EnsureCurrentTenant();
        var remoteById = BuildRemoteProductMap(await _hksService.GetUrunlerAsync(cancellationToken));
        await PersistGlobalProductsAsync(remoteById, cancellationToken);
        return await GetCurrentTenantProductsAsync(cancellationToken);
    }

    private Guid EnsureCurrentTenant()
    {
        if (_currentUserService.IsSystemAdmin || _currentUserService.MusteriId is null || _currentUserService.MusteriId == Guid.Empty)
        {
            throw new HksIntegrationException(
                "HKS urun listesi icin aktif sirket baglantisi gerekir.",
                StatusCodes.Status403Forbidden);
        }

        return _currentUserService.MusteriId.Value;
    }

    private static void Reactivate(HksUrun entity)
    {
        if (entity.AktifMi)
        {
            return;
        }

        entity.AktifMi = true;
        entity.SilinmeTarihi = null;
        entity.SilenKullanici = null;
    }

    private async Task PersistGlobalProductsAsync(
        IReadOnlyDictionary<int, HksSelectOptionDto> remoteById,
        CancellationToken cancellationToken)
    {
        var existingProducts = await _dbContext.HksUrunler
            .IgnoreQueryFilters()
            .ToListAsync(cancellationToken);

        ApplyRemoteProducts(existingProducts, remoteById);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private void ApplyRemoteProducts(
        List<HksUrun> existingProducts,
        IReadOnlyDictionary<int, HksSelectOptionDto> remoteById)
    {
        var user = _currentUserService.EPosta ?? "System";

        foreach (var remoteProduct in remoteById.Values)
        {
            var existing = existingProducts.FirstOrDefault(x => x.HksUrunId == remoteProduct.Id);
            if (existing is null)
            {
                var entity = new HksUrun
                {
                    Id = IdGenerator.CreateV7(),
                    HksUrunId = remoteProduct.Id,
                    Ad = remoteProduct.Ad.Trim(),
                    AktifMi = true,
                    KayitTarihi = DateTime.UtcNow,
                    OlusturanKullanici = user,
                    Source = AuditSource.System
                };

                _dbContext.HksUrunler.Add(entity);
                existingProducts.Add(entity);
                continue;
            }

            existing.Ad = remoteProduct.Ad.Trim();
            MarkUpdated(existing, user);
            Reactivate(existing);
        }

        foreach (var existing in existingProducts.Where(x => !remoteById.ContainsKey(x.HksUrunId)))
        {
            Deactivate(existing);
        }
    }

    private void Deactivate(HksUrun entity)
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

    private static void MarkUpdated(HksUrun entity, string user)
    {
        entity.GuncellemeTarihi = DateTime.UtcNow;
        entity.GuncelleyenKullanici = user;
        entity.Source = AuditSource.System;
    }

    private static IReadOnlyDictionary<int, HksSelectOptionDto> BuildRemoteProductMap(IReadOnlyList<HksSelectOptionDto> remoteProducts)
    {
        var remoteById = remoteProducts
            .Where(item => item.Id > 0 && !string.IsNullOrWhiteSpace(item.Ad))
            .GroupBy(item => item.Id)
            .Select(group => group.First())
            .ToDictionary(item => item.Id);

        if (remoteById.Count > 0)
        {
            return remoteById;
        }

        throw new HksIntegrationException(
            "HKS urun listesi bos dondu. Kayitlar guncellenmedi.",
            StatusCodes.Status502BadGateway);
    }

    private static HksUrunKayitDto ToDto(HksUrun entity)
    {
        return new HksUrunKayitDto
        {
            Id = entity.Id,
            HksUrunId = entity.HksUrunId,
            Ad = entity.Ad,
            GuncellemeTarihi = entity.GuncellemeTarihi ?? entity.KayitTarihi
        };
    }
}
