using AtlasWeb.Data;
using AtlasWeb.DTOs;
using AtlasWeb.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace AtlasWeb.Services;

public sealed class HksSifatService : IHksSifatService
{
    private readonly AtlasDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IHksService _hksService;

    public HksSifatService(
        AtlasDbContext dbContext,
        ICurrentUserService currentUserService,
        IHksService hksService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _hksService = hksService;
    }

    public async Task<IReadOnlyList<HksSifatKayitDto>> GetCurrentTenantSifatlarAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.HksSifatlar
            .IgnoreQueryFilters()
            .Where(x => x.AktifMi)
            .OrderBy(x => x.Ad)
            .Select(x => ToDto(x))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<HksSifatKayitDto>> SyncCurrentTenantSifatlarAsync(CancellationToken cancellationToken = default)
    {
        _ = EnsureCurrentTenant();
        var remoteById = BuildRemoteMap(await _hksService.GetSifatlarAsync(cancellationToken));
        await PersistGlobalSifatlarAsync(remoteById, cancellationToken);
        return await GetCurrentTenantSifatlarAsync(cancellationToken);
    }

    private Guid EnsureCurrentTenant()
    {
        if (_currentUserService.IsSystemAdmin || _currentUserService.MusteriId is null || _currentUserService.MusteriId == Guid.Empty)
        {
            throw new HksIntegrationException(
                "HKS sifat listesi icin aktif sirket baglantisi gerekir.",
                StatusCodes.Status403Forbidden);
        }

        return _currentUserService.MusteriId.Value;
    }

    private async Task PersistGlobalSifatlarAsync(
        IReadOnlyDictionary<int, HksSelectOptionDto> remoteById,
        CancellationToken cancellationToken)
    {
        var existingSifatlar = await _dbContext.HksSifatlar
            .IgnoreQueryFilters()
            .ToListAsync(cancellationToken);

        ApplyRemoteSifatlar(existingSifatlar, remoteById);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private void ApplyRemoteSifatlar(
        List<HksSifat> existingSifatlar,
        IReadOnlyDictionary<int, HksSelectOptionDto> remoteById)
    {
        var user = _currentUserService.EPosta ?? "System";

        foreach (var remoteSifat in remoteById.Values)
        {
            var existing = existingSifatlar.FirstOrDefault(x => x.HksSifatId == remoteSifat.Id);
            if (existing is null)
            {
                var entity = new HksSifat
                {
                    Id = IdGenerator.CreateV7(),
                    HksSifatId = remoteSifat.Id,
                    Ad = remoteSifat.Ad.Trim(),
                    AktifMi = true,
                    KayitTarihi = DateTime.UtcNow,
                    OlusturanKullanici = user,
                    Source = AuditSource.System
                };

                _dbContext.HksSifatlar.Add(entity);
                existingSifatlar.Add(entity);
                continue;
            }

            existing.Ad = remoteSifat.Ad.Trim();
            MarkUpdated(existing, user);
            Reactivate(existing);
        }

        foreach (var existing in existingSifatlar.Where(x => !remoteById.ContainsKey(x.HksSifatId)))
        {
            Deactivate(existing);
        }
    }

    private static void Reactivate(HksSifat entity)
    {
        if (entity.AktifMi)
        {
            return;
        }

        entity.AktifMi = true;
        entity.SilinmeTarihi = null;
        entity.SilenKullanici = null;
    }

    private void Deactivate(HksSifat entity)
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

    private static void MarkUpdated(HksSifat entity, string user)
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
            "HKS sifat listesi bos dondu. Kayitlar guncellenmedi.",
            StatusCodes.Status502BadGateway);
    }

    private static HksSifatKayitDto ToDto(HksSifat entity)
    {
        return new HksSifatKayitDto
        {
            Id = entity.Id,
            HksSifatId = entity.HksSifatId,
            Ad = entity.Ad,
            GuncellemeTarihi = entity.GuncellemeTarihi ?? entity.KayitTarihi
        };
    }
}
