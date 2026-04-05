using AtlasWeb.DTOs;
using AtlasWeb.Services;
using AtlasWeb.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace AtlasWeb.Tests;

public sealed class HksIlceServiceTests
{
    [Fact]
    public async Task SyncCurrentTenantDistrictsAsync_PersistsAndReactivatesSharedDistricts()
    {
        await using var harness = new AtlasTestContext();
        var tenant = await harness.CreateTenantAsync("HKSILCE1", "HKS Ilce");
        harness.SetUser(tenant.Id);

        harness.DbContext.HksIller.Add(new AtlasWeb.Models.HksIl
        {
            Id = IdGenerator.CreateV7(),
            HksIlId = 6,
            Ad = "ANKARA",
            AktifMi = true
        });

        harness.DbContext.HksIlceler.Add(new AtlasWeb.Models.HksIlce
        {
            Id = IdGenerator.CreateV7(),
            HksIlceId = 61,
            HksIlId = 6,
            Ad = "ESKI CANKAYA",
            AktifMi = false,
            SilinmeTarihi = DateTime.UtcNow,
            SilenKullanici = "test"
        });

        await harness.DbContext.SaveChangesAsync();

        var service = new HksIlceService(
            harness.DbContext,
            harness.CurrentUser,
            new StubHksService(
                districtMap: new Dictionary<int, IReadOnlyList<HksSelectOptionDto>>
                {
                    [6] =
                    [
                        new HksSelectOptionDto { Id = 61, Ad = "CANKAYA" },
                        new HksSelectOptionDto { Id = 62, Ad = "MAMAK" }
                    ]
                }),
            new HksIlService(harness.DbContext, harness.CurrentUser, new StubHksService()));

        var result = await service.SyncCurrentTenantDistrictsAsync();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, item => item.HksIlceId == 61 && item.HksIlId == 6 && item.Ad == "CANKAYA");
        Assert.Contains(result, item => item.HksIlceId == 62 && item.HksIlId == 6 && item.Ad == "MAMAK");

        var saved = await harness.DbContext.HksIlceler
            .IgnoreQueryFilters()
            .OrderBy(x => x.HksIlceId)
            .ToListAsync();

        Assert.Equal(2, saved.Count);
        Assert.All(saved, item => Assert.True(item.AktifMi));
    }

    [Fact]
    public async Task SyncCurrentTenantDistrictsAsync_DeactivatesMissingDistrictsOnFullRefresh()
    {
        await using var harness = new AtlasTestContext();
        var tenant = await harness.CreateTenantAsync("HKSILCE2", "HKS Ilce 2");
        harness.SetUser(tenant.Id);

        harness.DbContext.HksIller.Add(new AtlasWeb.Models.HksIl
        {
            Id = IdGenerator.CreateV7(),
            HksIlId = 6,
            Ad = "ANKARA",
            AktifMi = true
        });

        harness.DbContext.HksIlceler.AddRange(
            new AtlasWeb.Models.HksIlce
            {
                Id = IdGenerator.CreateV7(),
                HksIlceId = 61,
                HksIlId = 6,
                Ad = "CANKAYA",
                AktifMi = true
            },
            new AtlasWeb.Models.HksIlce
            {
                Id = IdGenerator.CreateV7(),
                HksIlceId = 62,
                HksIlId = 6,
                Ad = "MAMAK",
                AktifMi = true
            });

        await harness.DbContext.SaveChangesAsync();

        var service = new HksIlceService(
            harness.DbContext,
            harness.CurrentUser,
            new StubHksService(
                districtMap: new Dictionary<int, IReadOnlyList<HksSelectOptionDto>>
                {
                    [6] = [new HksSelectOptionDto { Id = 61, Ad = "CANKAYA" }]
                }),
            new HksIlService(harness.DbContext, harness.CurrentUser, new StubHksService()));

        var result = await service.SyncCurrentTenantDistrictsAsync();

        Assert.Single(result);
        Assert.Equal(61, result[0].HksIlceId);

        var deactivated = await harness.DbContext.HksIlceler
            .IgnoreQueryFilters()
            .SingleAsync(x => x.HksIlceId == 62);

        Assert.False(deactivated.AktifMi);
    }

    private sealed class StubHksService : IHksService
    {
        private readonly IReadOnlyDictionary<int, IReadOnlyList<HksSelectOptionDto>> _districtMap;

        public StubHksService(IReadOnlyDictionary<int, IReadOnlyList<HksSelectOptionDto>>? districtMap = null)
        {
            _districtMap = districtMap ?? new Dictionary<int, IReadOnlyList<HksSelectOptionDto>>();
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
            => Task.FromResult(_districtMap.TryGetValue(ilId, out var districts) ? districts : []);

        public Task<IReadOnlyList<HksSelectOptionDto>> GetIlcelerForTenantAsync(Guid tenantId, int ilId, CancellationToken cancellationToken = default)
            => GetIlcelerAsync(ilId, cancellationToken);

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
            => Task.FromResult<IReadOnlyList<HksSelectOptionDto>>([]);

        public Task<IReadOnlyList<HksUrunCinsiDto>> GetUrunCinsleriAsync(int urunId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<HksUrunCinsiDto>>([]);

        public Task<HksReferansKunyeResponseDto> GetReferansKunyelerAsync(HksReferansKunyeRequestDto request, CancellationToken cancellationToken = default)
            => Task.FromResult(new HksReferansKunyeResponseDto());

        public Task<HksReferansKunyeResponseDto> GetReferansKunyelerForTenantAsync(Guid tenantId, HksReferansKunyeRequestDto request, Func<HksSearchProgressDto, Task>? progressCallback = null, CancellationToken cancellationToken = default)
            => Task.FromResult(new HksReferansKunyeResponseDto());
    }
}
