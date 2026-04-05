using AtlasWeb.DTOs;
using AtlasWeb.Services;
using AtlasWeb.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace AtlasWeb.Tests;

public sealed class HksUretimSekliServiceTests
{
    [Fact]
    public async Task SyncCurrentTenantProductionShapesAsync_PersistsAndReactivatesSharedShapes()
    {
        await using var harness = new AtlasTestContext();
        var tenant = await harness.CreateTenantAsync("HKSUS1", "HKS Uretim Sekli");
        harness.SetUser(tenant.Id);

        harness.DbContext.HksUretimSekilleri.Add(new AtlasWeb.Models.HksUretimSekli
        {
            Id = IdGenerator.CreateV7(),
            HksUretimSekliId = 10,
            Ad = "ESKI SEKIL",
            AktifMi = false,
            SilinmeTarihi = DateTime.UtcNow,
            SilenKullanici = "test"
        });

        await harness.DbContext.SaveChangesAsync();

        var service = new HksUretimSekliService(
            harness.DbContext,
            harness.CurrentUser,
            new StubHksService(
            [
                new HksSelectOptionDto { Id = 10, Ad = "GELENEKSEL" },
                new HksSelectOptionDto { Id = 11, Ad = "ORGANIK" }
            ]));

        var result = await service.SyncCurrentTenantProductionShapesAsync();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, item => item.HksUretimSekliId == 10 && item.Ad == "GELENEKSEL");
        Assert.Contains(result, item => item.HksUretimSekliId == 11 && item.Ad == "ORGANIK");

        var saved = await harness.DbContext.HksUretimSekilleri
            .IgnoreQueryFilters()
            .OrderBy(x => x.HksUretimSekliId)
            .ToListAsync();

        Assert.Equal(2, saved.Count);
        Assert.All(saved, item => Assert.True(item.AktifMi));
    }

    [Fact]
    public async Task SyncCurrentTenantProductionShapesAsync_DeactivatesMissingShapes()
    {
        await using var harness = new AtlasTestContext();
        var tenant = await harness.CreateTenantAsync("HKSUS2", "HKS Uretim Sekli 2");
        harness.SetUser(tenant.Id);

        harness.DbContext.HksUretimSekilleri.AddRange(
            new AtlasWeb.Models.HksUretimSekli
            {
                Id = IdGenerator.CreateV7(),
                HksUretimSekliId = 10,
                Ad = "GELENEKSEL",
                AktifMi = true
            },
            new AtlasWeb.Models.HksUretimSekli
            {
                Id = IdGenerator.CreateV7(),
                HksUretimSekliId = 11,
                Ad = "ORGANIK",
                AktifMi = true
            });

        await harness.DbContext.SaveChangesAsync();

        var service = new HksUretimSekliService(
            harness.DbContext,
            harness.CurrentUser,
            new StubHksService([new HksSelectOptionDto { Id = 10, Ad = "GELENEKSEL" }]));

        var result = await service.SyncCurrentTenantProductionShapesAsync();

        Assert.Single(result);
        Assert.Equal(10, result[0].HksUretimSekliId);

        var deactivated = await harness.DbContext.HksUretimSekilleri
            .IgnoreQueryFilters()
            .SingleAsync(x => x.HksUretimSekliId == 11);

        Assert.False(deactivated.AktifMi);
    }

    private sealed class StubHksService : IHksService
    {
        private readonly IReadOnlyList<HksSelectOptionDto> _shapes;

        public StubHksService(IReadOnlyList<HksSelectOptionDto> shapes)
        {
            _shapes = shapes;
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
            => Task.FromResult<IReadOnlyList<HksSelectOptionDto>>([]);

        public Task<IReadOnlyList<HksSelectOptionDto>> GetIsletmeTurleriAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<HksSelectOptionDto>>([]);

        public Task<IReadOnlyList<HksSelectOptionDto>> GetUretimSekilleriAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(_shapes);

        public Task<IReadOnlyList<HksUrunCinsiDto>> GetUrunCinsleriAsync(int urunId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<HksUrunCinsiDto>>([]);

        public Task<HksReferansKunyeResponseDto> GetReferansKunyelerAsync(HksReferansKunyeRequestDto request, CancellationToken cancellationToken = default)
            => Task.FromResult(new HksReferansKunyeResponseDto());

        public Task<HksReferansKunyeResponseDto> GetReferansKunyelerForTenantAsync(Guid tenantId, HksReferansKunyeRequestDto request, Func<HksSearchProgressDto, Task>? progressCallback = null, CancellationToken cancellationToken = default)
            => Task.FromResult(new HksReferansKunyeResponseDto());
    }
}
