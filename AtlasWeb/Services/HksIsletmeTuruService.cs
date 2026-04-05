using AtlasWeb.Data;
using AtlasWeb.DTOs;
using AtlasWeb.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace AtlasWeb.Services;

public sealed class HksIsletmeTuruService : IHksIsletmeTuruService
{
    private readonly AtlasDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IHksService _hksService;

    public HksIsletmeTuruService(
        AtlasDbContext dbContext,
        ICurrentUserService currentUserService,
        IHksService hksService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _hksService = hksService;
    }

    public async Task<IReadOnlyList<HksIsletmeTuruKayitDto>> GetCurrentTenantBusinessTypesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.HksIsletmeTurleri
            .IgnoreQueryFilters()
            .Where(x => x.AktifMi)
            .OrderBy(x => x.Ad)
            .Select(x => ToDto(x))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<HksIsletmeTuruKayitDto>> SyncCurrentTenantBusinessTypesAsync(CancellationToken cancellationToken = default)
    {
        _ = EnsureCurrentTenant();
        var remoteById = BuildRemoteMap(await _hksService.GetIsletmeTurleriAsync(cancellationToken));
        await PersistGlobalBusinessTypesAsync(remoteById, cancellationToken);
        return await GetCurrentTenantBusinessTypesAsync(cancellationToken);
    }

    private Guid EnsureCurrentTenant()
    {
        if (_currentUserService.IsSystemAdmin || _currentUserService.MusteriId is null || _currentUserService.MusteriId == Guid.Empty)
        {
            throw new HksIntegrationException(
                "HKS isletme turu listesi icin aktif sirket baglantisi gerekir.",
                StatusCodes.Status403Forbidden);
        }

        return _currentUserService.MusteriId.Value;
    }

    private async Task PersistGlobalBusinessTypesAsync(
        IReadOnlyDictionary<int, HksSelectOptionDto> remoteById,
        CancellationToken cancellationToken)
    {
        var existingBusinessTypes = await _dbContext.HksIsletmeTurleri
            .IgnoreQueryFilters()
            .ToListAsync(cancellationToken);

        ApplyRemoteBusinessTypes(existingBusinessTypes, remoteById);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private void ApplyRemoteBusinessTypes(
        List<HksIsletmeTuru> existingBusinessTypes,
        IReadOnlyDictionary<int, HksSelectOptionDto> remoteById)
    {
        var user = _currentUserService.EPosta ?? "System";

        foreach (var remoteBusinessType in remoteById.Values)
        {
            var existing = existingBusinessTypes.FirstOrDefault(x => x.HksIsletmeTuruId == remoteBusinessType.Id);
            if (existing is null)
            {
                var entity = new HksIsletmeTuru
                {
                    Id = IdGenerator.CreateV7(),
                    HksIsletmeTuruId = remoteBusinessType.Id,
                    Ad = remoteBusinessType.Ad.Trim(),
                    AktifMi = true,
                    KayitTarihi = DateTime.UtcNow,
                    OlusturanKullanici = user,
                    Source = AuditSource.System
                };

                _dbContext.HksIsletmeTurleri.Add(entity);
                existingBusinessTypes.Add(entity);
                continue;
            }

            existing.Ad = remoteBusinessType.Ad.Trim();
            MarkUpdated(existing, user);
            Reactivate(existing);
        }

        foreach (var existing in existingBusinessTypes.Where(x => !remoteById.ContainsKey(x.HksIsletmeTuruId)))
        {
            Deactivate(existing);
        }
    }

    private static void Reactivate(HksIsletmeTuru entity)
    {
        if (entity.AktifMi)
        {
            return;
        }

        entity.AktifMi = true;
        entity.SilinmeTarihi = null;
        entity.SilenKullanici = null;
    }

    private void Deactivate(HksIsletmeTuru entity)
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

    private static void MarkUpdated(HksIsletmeTuru entity, string user)
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
            "HKS isletme turu listesi bos dondu. Kayitlar guncellenmedi.",
            StatusCodes.Status502BadGateway);
    }

    private static HksIsletmeTuruKayitDto ToDto(HksIsletmeTuru entity)
    {
        return new HksIsletmeTuruKayitDto
        {
            Id = entity.Id,
            HksIsletmeTuruId = entity.HksIsletmeTuruId,
            Ad = entity.Ad,
            GuncellemeTarihi = entity.GuncellemeTarihi ?? entity.KayitTarihi
        };
    }
}
