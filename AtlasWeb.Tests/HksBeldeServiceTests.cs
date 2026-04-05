using AtlasWeb.DTOs;
using AtlasWeb.Services;
using AtlasWeb.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace AtlasWeb.Tests;

public sealed class HksBeldeServiceTests
{
    [Fact]
    public async Task SyncCurrentTenantTownsAsync_PersistsAndReactivatesSharedTowns()
    {
        await using var harness = new AtlasTestContext();
        var tenant = await harness.CreateTenantAsync("HKSBELDE1", "HKS Belde");
        harness.SetUser(tenant.Id);

        harness.DbContext.HksIlceler.Add(new AtlasWeb.Models.HksIlce
        {
            Id = IdGenerator.CreateV7(),
            HksIlceId = 61,
            HksIlId = 6,
            Ad = "CANKAYA",
            AktifMi = true
        });

        harness.DbContext.HksBeldeler.Add(new AtlasWeb.Models.HksBelde
        {
            Id = IdGenerator.CreateV7(),
            HksBeldeId = 6101,
            HksIlceId = 61,
            Ad = "ESKI BELDE",
            AktifMi = false,
            SilinmeTarihi = DateTime.UtcNow,
            SilenKullanici = "test"
        });

        await harness.DbContext.SaveChangesAsync();

        var hksService = new StubHksService(
            townMap: new Dictionary<int, IReadOnlyList<HksSelectOptionDto>>
            {
                [61] =
                [
                    new HksSelectOptionDto { Id = 6101, Ad = "KARSIYAKA" },
                    new HksSelectOptionDto { Id = 6102, Ad = "ORNEK BELDE" }
                ]
            });

        var ilceService = new HksIlceService(
            harness.DbContext,
            harness.CurrentUser,
            hksService,
            new HksIlService(harness.DbContext, harness.CurrentUser, new StubHksService()));

        var service = new HksBeldeService(
            harness.DbContext,
            harness.CurrentUser,
            hksService,
            ilceService);

        var result = await service.SyncCurrentTenantTownsAsync();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, item => item.HksBeldeId == 6101 && item.HksIlceId == 61 && item.Ad == "KARSIYAKA");
        Assert.Contains(result, item => item.HksBeldeId == 6102 && item.HksIlceId == 61 && item.Ad == "ORNEK BELDE");

        var saved = await harness.DbContext.HksBeldeler
            .IgnoreQueryFilters()
            .OrderBy(x => x.HksBeldeId)
            .ToListAsync();

        Assert.Equal(2, saved.Count);
        Assert.All(saved, item => Assert.True(item.AktifMi));
    }

    [Fact]
    public async Task SyncCurrentTenantTownsAsync_DeactivatesMissingTownsOnFullRefresh()
    {
        await using var harness = new AtlasTestContext();
        var tenant = await harness.CreateTenantAsync("HKSBELDE2", "HKS Belde 2");
        harness.SetUser(tenant.Id);

        harness.DbContext.HksIlceler.Add(new AtlasWeb.Models.HksIlce
        {
            Id = IdGenerator.CreateV7(),
            HksIlceId = 61,
            HksIlId = 6,
            Ad = "CANKAYA",
            AktifMi = true
        });

        harness.DbContext.HksBeldeler.AddRange(
            new AtlasWeb.Models.HksBelde
            {
                Id = IdGenerator.CreateV7(),
                HksBeldeId = 6101,
                HksIlceId = 61,
                Ad = "KARSIYAKA",
                AktifMi = true
            },
            new AtlasWeb.Models.HksBelde
            {
                Id = IdGenerator.CreateV7(),
                HksBeldeId = 6102,
                HksIlceId = 61,
                Ad = "ORNEK BELDE",
                AktifMi = true
            });

        await harness.DbContext.SaveChangesAsync();

        var hksService = new StubHksService(
            townMap: new Dictionary<int, IReadOnlyList<HksSelectOptionDto>>
            {
                [61] = [new HksSelectOptionDto { Id = 6101, Ad = "KARSIYAKA" }]
            });

        var ilceService = new HksIlceService(
            harness.DbContext,
            harness.CurrentUser,
            hksService,
            new HksIlService(harness.DbContext, harness.CurrentUser, new StubHksService()));

        var service = new HksBeldeService(
            harness.DbContext,
            harness.CurrentUser,
            hksService,
            ilceService);

        var result = await service.SyncCurrentTenantTownsAsync();

        Assert.Single(result);
        Assert.Equal(6101, result[0].HksBeldeId);

        var deactivated = await harness.DbContext.HksBeldeler
            .IgnoreQueryFilters()
            .SingleAsync(x => x.HksBeldeId == 6102);

        Assert.False(deactivated.AktifMi);
    }

    private sealed class StubHksService : IHksService
    {
        private readonly IReadOnlyDictionary<int, IReadOnlyList<HksSelectOptionDto>> _townMap;

        public StubHksService(IReadOnlyDictionary<int, IReadOnlyList<HksSelectOptionDto>>? townMap = null)
        {
            _townMap = townMap ?? new Dictionary<int, IReadOnlyList<HksSelectOptionDto>>();
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
            => Task.FromResult(_townMap.TryGetValue(ilceId, out var towns) ? towns : []);

        public Task<IReadOnlyList<HksSelectOptionDto>> GetBeldelerForTenantAsync(Guid tenantId, int ilceId, CancellationToken cancellationToken = default)
            => GetBeldelerAsync(ilceId, cancellationToken);

        public Task<IReadOnlyList<HksSelectOptionDto>> GetUrunlerAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<HksSelectOptionDto>>([]);

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
