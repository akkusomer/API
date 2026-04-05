using AtlasWeb.DTOs;
using AtlasWeb.Services;
using AtlasWeb.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace AtlasWeb.Tests;

public sealed class HksUrunBirimServiceTests
{
    [Fact]
    public async Task SyncCurrentTenantProductUnitsAsync_PersistsAndReactivatesSharedUnits()
    {
        await using var harness = new AtlasTestContext();
        var tenant = await harness.CreateTenantAsync("HKSUB1", "HKS Urun Birim");
        harness.SetUser(tenant.Id);

        harness.DbContext.HksUrunBirimleri.Add(new AtlasWeb.Models.HksUrunBirim
        {
            Id = IdGenerator.CreateV7(),
            HksUrunBirimId = 1,
            Ad = "ESKI KG",
            AktifMi = false,
            SilinmeTarihi = DateTime.UtcNow,
            SilenKullanici = "test"
        });

        await harness.DbContext.SaveChangesAsync();

        var service = new HksUrunBirimService(
            harness.DbContext,
            harness.CurrentUser,
            new StubHksService(
            [
                new HksSelectOptionDto { Id = 1, Ad = "KILOGRAM" },
                new HksSelectOptionDto { Id = 2, Ad = "ADET" }
            ]));

        var result = await service.SyncCurrentTenantProductUnitsAsync();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, item => item.HksUrunBirimId == 1 && item.Ad == "KILOGRAM");
        Assert.Contains(result, item => item.HksUrunBirimId == 2 && item.Ad == "ADET");

        var saved = await harness.DbContext.HksUrunBirimleri
            .IgnoreQueryFilters()
            .OrderBy(x => x.HksUrunBirimId)
            .ToListAsync();

        Assert.Equal(2, saved.Count);
        Assert.All(saved, item => Assert.True(item.AktifMi));
    }

    [Fact]
    public async Task SyncCurrentTenantProductUnitsAsync_DeactivatesMissingUnits()
    {
        await using var harness = new AtlasTestContext();
        var tenant = await harness.CreateTenantAsync("HKSUB2", "HKS Urun Birim 2");
        harness.SetUser(tenant.Id);

        harness.DbContext.HksUrunBirimleri.AddRange(
            new AtlasWeb.Models.HksUrunBirim
            {
                Id = IdGenerator.CreateV7(),
                HksUrunBirimId = 1,
                Ad = "KILOGRAM",
                AktifMi = true
            },
            new AtlasWeb.Models.HksUrunBirim
            {
                Id = IdGenerator.CreateV7(),
                HksUrunBirimId = 2,
                Ad = "ADET",
                AktifMi = true
            });

        await harness.DbContext.SaveChangesAsync();

        var service = new HksUrunBirimService(
            harness.DbContext,
            harness.CurrentUser,
            new StubHksService([new HksSelectOptionDto { Id = 1, Ad = "KILOGRAM" }]));

        var result = await service.SyncCurrentTenantProductUnitsAsync();

        Assert.Single(result);
        Assert.Equal(1, result[0].HksUrunBirimId);

        var deactivated = await harness.DbContext.HksUrunBirimleri
            .IgnoreQueryFilters()
            .SingleAsync(x => x.HksUrunBirimId == 2);

        Assert.False(deactivated.AktifMi);
    }

    private sealed class StubHksService : IHksService
    {
        private readonly IReadOnlyList<HksSelectOptionDto> _units;

        public StubHksService(IReadOnlyList<HksSelectOptionDto> units)
        {
            _units = units;
        }

        public Task<IReadOnlyList<HksSelectOptionDto>> GetSifatlarAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<HksSelectOptionDto>>([]);

        public Task<HksKayitliKisiSorguDto?> GetKayitliKisiSorguAsync(string tcKimlikVergiNo, CancellationToken cancellationToken = default)
            => Task.FromResult<HksKayitliKisiSorguDto?>(null);

        public Task<IReadOnlyList<HksSelectOptionDto>> GetIllerAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<HksSelectOptionDto>>([]);

        public Task<IReadOnlyList<HksSelectOptionDto>> GetIllerForTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<HksSelectOptionDto>>([]);

        public Task<IReadOnlyList<HksSelectOptionDto>> GetIlcelerAsync(int ilId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<HksSelectOptionDto>>([]);

        public Task<IReadOnlyList<HksSelectOptionDto>> GetIlcelerForTenantAsync(Guid tenantId, int ilId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<HksSelectOptionDto>>([]);

        public Task<IReadOnlyList<HksSelectOptionDto>> GetBeldelerAsync(int ilceId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<HksSelectOptionDto>>([]);

        public Task<IReadOnlyList<HksSelectOptionDto>> GetBeldelerForTenantAsync(Guid tenantId, int ilceId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<HksSelectOptionDto>>([]);

        public Task<IReadOnlyList<HksSelectOptionDto>> GetUrunlerAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<HksSelectOptionDto>>([]);

        public Task<IReadOnlyList<HksSelectOptionDto>> GetUrunBirimleriAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(_units);

        public Task<IReadOnlyList<HksSelectOptionDto>> GetIsletmeTurleriAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<HksSelectOptionDto>>([]);

        public Task<IReadOnlyList<HksSelectOptionDto>> GetUretimSekilleriAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<HksSelectOptionDto>>([]);

        public Task<IReadOnlyList<HksUrunCinsiDto>> GetUrunCinsleriAsync(int urunId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<HksUrunCinsiDto>>([]);

        public Task<HksReferansKunyeResponseDto> GetReferansKunyelerAsync(HksReferansKunyeRequestDto request, CancellationToken cancellationToken = default)
            => Task.FromResult(new HksReferansKunyeResponseDto());

        public Task<HksReferansKunyeResponseDto> GetReferansKunyelerForTenantAsync(Guid tenantId, HksReferansKunyeRequestDto request, Func<HksSearchProgressDto, Task>? progressCallback = null, CancellationToken cancellationToken = default)
            => Task.FromResult(new HksReferansKunyeResponseDto());
    }
}
