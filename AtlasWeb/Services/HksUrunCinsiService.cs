using AtlasWeb.Data;
using AtlasWeb.DTOs;
using AtlasWeb.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace AtlasWeb.Services;

public sealed class HksUrunCinsiService : IHksUrunCinsiService
{
    private readonly AtlasDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IHksService _hksService;
    private readonly IHksUrunService _hksUrunService;

    public HksUrunCinsiService(
        AtlasDbContext dbContext,
        ICurrentUserService currentUserService,
        IHksService hksService,
        IHksUrunService hksUrunService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _hksService = hksService;
        _hksUrunService = hksUrunService;
    }

    public async Task<IReadOnlyList<HksUrunCinsiKayitDto>> GetCurrentTenantProductKindsAsync(int? urunId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.HksUrunCinsleri
            .IgnoreQueryFilters()
            .Where(x => x.AktifMi);

        if (urunId.HasValue)
        {
            query = query.Where(x => x.HksUrunId == urunId.Value);
        }

        return await query
            .OrderBy(x => x.Ad)
            .Select(x => ToDto(x))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<HksUrunCinsiKayitDto>> SyncCurrentTenantProductKindsAsync(int? urunId = null, CancellationToken cancellationToken = default)
    {
        _ = EnsureCurrentTenant();
        var products = await EnsureProductsAsync(urunId, cancellationToken);
        if (products.Count == 0)
        {
            return [];
        }

        var remoteById = await BuildRemoteKindMapAsync(products, cancellationToken);
        if (urunId is null && remoteById.Count == 0)
        {
            throw new HksIntegrationException(
                "HKS urun cinsi listesi bos dondu. Kayitlar guncellenmedi.",
                StatusCodes.Status502BadGateway);
        }

        await PersistGlobalProductKindsAsync(
            remoteById,
            products.Select(x => x.HksUrunId).ToHashSet(),
            fullRefresh: urunId is null,
            cancellationToken);

        return await GetCurrentTenantProductKindsAsync(urunId, cancellationToken);
    }

    private Guid EnsureCurrentTenant()
    {
        if (_currentUserService.IsSystemAdmin || _currentUserService.MusteriId is null || _currentUserService.MusteriId == Guid.Empty)
        {
            throw new HksIntegrationException(
                "HKS urun cinsi listesi icin aktif sirket baglantisi gerekir.",
                StatusCodes.Status403Forbidden);
        }

        return _currentUserService.MusteriId.Value;
    }

    private async Task<List<HksUrunKayitDto>> EnsureProductsAsync(int? urunId, CancellationToken cancellationToken)
    {
        var products = await LoadSavedProductsAsync(urunId, cancellationToken);
        if (products.Count > 0)
        {
            return products;
        }

        await _hksUrunService.SyncCurrentTenantProductsAsync(cancellationToken);
        return await LoadSavedProductsAsync(urunId, cancellationToken);
    }

    private async Task<List<HksUrunKayitDto>> LoadSavedProductsAsync(int? urunId, CancellationToken cancellationToken)
    {
        var query = _dbContext.HksUrunler
            .IgnoreQueryFilters()
            .Where(x => x.AktifMi);

        if (urunId.HasValue)
        {
            query = query.Where(x => x.HksUrunId == urunId.Value);
        }

        return await query
            .OrderBy(x => x.HksUrunId)
            .Select(x => new HksUrunKayitDto
            {
                Id = x.Id,
                HksUrunId = x.HksUrunId,
                Ad = x.Ad,
                GuncellemeTarihi = x.GuncellemeTarihi ?? x.KayitTarihi
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<Dictionary<int, HksUrunCinsiDto>> BuildRemoteKindMapAsync(
        IReadOnlyList<HksUrunKayitDto> products,
        CancellationToken cancellationToken)
    {
        var remoteById = new Dictionary<int, HksUrunCinsiDto>();

        foreach (var product in products)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var kinds = await _hksService.GetUrunCinsleriAsync(product.HksUrunId, cancellationToken);
            foreach (var kind in kinds.Where(item => item.HksUrunCinsiId > 0 && item.HksUrunId > 0 && !string.IsNullOrWhiteSpace(item.Ad)))
            {
                remoteById[kind.HksUrunCinsiId] = kind;
            }
        }

        return remoteById;
    }

    private async Task PersistGlobalProductKindsAsync(
        IReadOnlyDictionary<int, HksUrunCinsiDto> remoteById,
        IReadOnlySet<int> scopedProductIds,
        bool fullRefresh,
        CancellationToken cancellationToken)
    {
        var existingKinds = await _dbContext.HksUrunCinsleri
            .IgnoreQueryFilters()
            .ToListAsync(cancellationToken);

        ApplyRemoteProductKinds(existingKinds, remoteById, scopedProductIds, fullRefresh);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private void ApplyRemoteProductKinds(
        List<HksUrunCinsi> existingKinds,
        IReadOnlyDictionary<int, HksUrunCinsiDto> remoteById,
        IReadOnlySet<int> scopedProductIds,
        bool fullRefresh)
    {
        var user = _currentUserService.EPosta ?? "System";

        foreach (var remoteKind in remoteById.Values)
        {
            var existing = existingKinds.FirstOrDefault(x => x.HksUrunCinsiId == remoteKind.HksUrunCinsiId);
            if (existing is null)
            {
                var entity = new HksUrunCinsi
                {
                    Id = IdGenerator.CreateV7(),
                    HksUrunCinsiId = remoteKind.HksUrunCinsiId,
                    HksUrunId = remoteKind.HksUrunId,
                    HksUretimSekliId = remoteKind.HksUretimSekliId,
                    Ad = remoteKind.Ad.Trim(),
                    UrunKodu = remoteKind.UrunKodu?.Trim(),
                    IthalMi = remoteKind.IthalMi,
                    AktifMi = true,
                    KayitTarihi = DateTime.UtcNow,
                    OlusturanKullanici = user,
                    Source = AuditSource.System
                };

                _dbContext.HksUrunCinsleri.Add(entity);
                existingKinds.Add(entity);
                continue;
            }

            existing.HksUrunId = remoteKind.HksUrunId;
            existing.HksUretimSekliId = remoteKind.HksUretimSekliId;
            existing.Ad = remoteKind.Ad.Trim();
            existing.UrunKodu = remoteKind.UrunKodu?.Trim();
            existing.IthalMi = remoteKind.IthalMi;
            MarkUpdated(existing, user);
            Reactivate(existing);
        }

        var missingKinds = fullRefresh
            ? existingKinds.Where(x => !remoteById.ContainsKey(x.HksUrunCinsiId))
            : existingKinds.Where(x => scopedProductIds.Contains(x.HksUrunId) && !remoteById.ContainsKey(x.HksUrunCinsiId));

        foreach (var existing in missingKinds)
        {
            Deactivate(existing);
        }
    }

    private static void Reactivate(HksUrunCinsi entity)
    {
        if (entity.AktifMi)
        {
            return;
        }

        entity.AktifMi = true;
        entity.SilinmeTarihi = null;
        entity.SilenKullanici = null;
    }

    private void Deactivate(HksUrunCinsi entity)
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

    private static void MarkUpdated(HksUrunCinsi entity, string user)
    {
        entity.GuncellemeTarihi = DateTime.UtcNow;
        entity.GuncelleyenKullanici = user;
        entity.Source = AuditSource.System;
    }

    private static HksUrunCinsiKayitDto ToDto(HksUrunCinsi entity)
    {
        return new HksUrunCinsiKayitDto
        {
            Id = entity.Id,
            HksUrunCinsiId = entity.HksUrunCinsiId,
            HksUrunId = entity.HksUrunId,
            HksUretimSekliId = entity.HksUretimSekliId,
            Ad = entity.Ad,
            UrunKodu = entity.UrunKodu,
            IthalMi = entity.IthalMi,
            GuncellemeTarihi = entity.GuncellemeTarihi ?? entity.KayitTarihi
        };
    }
}
