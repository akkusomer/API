using AtlasWeb.DTOs;
using AtlasWeb.Services;
using AtlasWeb.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace AtlasWeb.Tests;

public sealed class HksUrunServiceTests
{
    [Fact]
    public async Task SyncCurrentTenantProductsAsync_PersistsAndReactivatesSharedProducts()
    {
        await using var harness = new AtlasTestContext();
        var tenant = await harness.CreateTenantAsync("HKSURUN1", "HKS Urun");
        harness.SetUser(tenant.Id);

        harness.DbContext.HksUrunler.Add(new AtlasWeb.Models.HksUrun
        {
            Id = IdGenerator.CreateV7(),
            HksUrunId = 300,
            Ad = "ESKI DOMATES",
            AktifMi = false,
            SilinmeTarihi = DateTime.UtcNow,
            SilenKullanici = "test"
        });

        await harness.DbContext.SaveChangesAsync();

        var service = new HksUrunService(
            harness.DbContext,
            harness.CurrentUser,
            new StubHksService([
                new HksSelectOptionDto { Id = 300, Ad = "DOMATES" },
                new HksSelectOptionDto { Id = 301, Ad = "BIBER" }
            ]));

        var result = await service.SyncCurrentTenantProductsAsync();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, item => item.HksUrunId == 300 && item.Ad == "DOMATES");
        Assert.Contains(result, item => item.HksUrunId == 301 && item.Ad == "BIBER");

        var saved = await harness.DbContext.HksUrunler
            .IgnoreQueryFilters()
            .OrderBy(x => x.HksUrunId)
            .ToListAsync();

        Assert.Equal(2, saved.Count);
        Assert.All(saved, item => Assert.True(item.AktifMi));
    }

    [Fact]
    public async Task SyncCurrentTenantProductsAsync_DeactivatesProductsMissingFromRemoteList()
    {
        await using var harness = new AtlasTestContext();
        var tenant = await harness.CreateTenantAsync("HKSURUN2", "HKS Urun 2");
        harness.SetUser(tenant.Id);

        harness.DbContext.HksUrunler.AddRange(
            new AtlasWeb.Models.HksUrun
            {
                Id = IdGenerator.CreateV7(),
                HksUrunId = 300,
                Ad = "DOMATES",
                AktifMi = true
            },
            new AtlasWeb.Models.HksUrun
            {
                Id = IdGenerator.CreateV7(),
                HksUrunId = 301,
                Ad = "BIBER",
                AktifMi = true
            });

        await harness.DbContext.SaveChangesAsync();

        var service = new HksUrunService(
            harness.DbContext,
            harness.CurrentUser,
            new StubHksService([
                new HksSelectOptionDto { Id = 300, Ad = "DOMATES" }
            ]));

        var result = await service.SyncCurrentTenantProductsAsync();

        Assert.Single(result);
        Assert.Equal(300, result[0].HksUrunId);

        var deactivated = await harness.DbContext.HksUrunler
            .IgnoreQueryFilters()
            .SingleAsync(x => x.HksUrunId == 301);

        Assert.False(deactivated.AktifMi);
    }

    private sealed class StubHksService : IHksService
    {
        private readonly IReadOnlyList<HksSelectOptionDto> _products;

        public StubHksService(IReadOnlyList<HksSelectOptionDto> products)
        {
            _products = products;
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
            => Task.FromResult(_products);

        public Task<IReadOnlyList<HksSelectOptionDto>> GetUrunBirimleriAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<HksSelectOptionDto>>([]);

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
