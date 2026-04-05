using AtlasWeb.DTOs;
using AtlasWeb.Services;
using AtlasWeb.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace AtlasWeb.Tests;

public sealed class HksUrunCinsiServiceTests
{
    [Fact]
    public async Task SyncCurrentTenantProductKindsAsync_PersistsAndReactivatesSharedKinds()
    {
        await using var harness = new AtlasTestContext();
        var tenant = await harness.CreateTenantAsync("HKSUC1", "HKS Urun Cinsi");
        harness.SetUser(tenant.Id);

        harness.DbContext.HksUrunler.Add(new AtlasWeb.Models.HksUrun
        {
            Id = IdGenerator.CreateV7(),
            HksUrunId = 300,
            Ad = "DOMATES",
            AktifMi = true
        });

        harness.DbContext.HksUrunCinsleri.Add(new AtlasWeb.Models.HksUrunCinsi
        {
            Id = IdGenerator.CreateV7(),
            HksUrunCinsiId = 1001,
            HksUrunId = 300,
            HksUretimSekliId = 10,
            Ad = "ESKI CINS",
            AktifMi = false,
            SilinmeTarihi = DateTime.UtcNow,
            SilenKullanici = "test"
        });

        await harness.DbContext.SaveChangesAsync();

        var service = new HksUrunCinsiService(
            harness.DbContext,
            harness.CurrentUser,
            new StubHksService(
                new Dictionary<int, IReadOnlyList<HksUrunCinsiDto>>
                {
                    [300] =
                    [
                        new HksUrunCinsiDto
                        {
                            HksUrunCinsiId = 1001,
                            HksUrunId = 300,
                            HksUretimSekliId = 10,
                            Ad = "SERA",
                            UrunKodu = "DMT-SR",
                            IthalMi = false
                        },
                        new HksUrunCinsiDto
                        {
                            HksUrunCinsiId = 1002,
                            HksUrunId = 300,
                            HksUretimSekliId = 11,
                            Ad = "ITHAL",
                            UrunKodu = "DMT-ITH",
                            IthalMi = true
                        }
                    ]
                }),
            new StubHksUrunService());

        var result = await service.SyncCurrentTenantProductKindsAsync();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, item => item.HksUrunCinsiId == 1001 && item.Ad == "SERA");
        Assert.Contains(result, item => item.HksUrunCinsiId == 1002 && item.IthalMi == true);

        var saved = await harness.DbContext.HksUrunCinsleri
            .IgnoreQueryFilters()
            .OrderBy(x => x.HksUrunCinsiId)
            .ToListAsync();

        Assert.Equal(2, saved.Count);
        Assert.All(saved, item => Assert.True(item.AktifMi));
    }

    [Fact]
    public async Task SyncCurrentTenantProductKindsAsync_DeactivatesMissingKindsOnFullRefresh()
    {
        await using var harness = new AtlasTestContext();
        var tenant = await harness.CreateTenantAsync("HKSUC2", "HKS Urun Cinsi 2");
        harness.SetUser(tenant.Id);

        harness.DbContext.HksUrunler.Add(new AtlasWeb.Models.HksUrun
        {
            Id = IdGenerator.CreateV7(),
            HksUrunId = 300,
            Ad = "DOMATES",
            AktifMi = true
        });

        harness.DbContext.HksUrunCinsleri.AddRange(
            new AtlasWeb.Models.HksUrunCinsi
            {
                Id = IdGenerator.CreateV7(),
                HksUrunCinsiId = 1001,
                HksUrunId = 300,
                HksUretimSekliId = 10,
                Ad = "SERA",
                AktifMi = true
            },
            new AtlasWeb.Models.HksUrunCinsi
            {
                Id = IdGenerator.CreateV7(),
                HksUrunCinsiId = 1002,
                HksUrunId = 300,
                HksUretimSekliId = 11,
                Ad = "ITHAL",
                AktifMi = true
            });

        await harness.DbContext.SaveChangesAsync();

        var service = new HksUrunCinsiService(
            harness.DbContext,
            harness.CurrentUser,
            new StubHksService(
                new Dictionary<int, IReadOnlyList<HksUrunCinsiDto>>
                {
                    [300] =
                    [
                        new HksUrunCinsiDto
                        {
                            HksUrunCinsiId = 1001,
                            HksUrunId = 300,
                            HksUretimSekliId = 10,
                            Ad = "SERA",
                            UrunKodu = "DMT-SR",
                            IthalMi = false
                        }
                    ]
                }),
            new StubHksUrunService());

        var result = await service.SyncCurrentTenantProductKindsAsync();

        Assert.Single(result);
        Assert.Equal(1001, result[0].HksUrunCinsiId);

        var deactivated = await harness.DbContext.HksUrunCinsleri
            .IgnoreQueryFilters()
            .SingleAsync(x => x.HksUrunCinsiId == 1002);

        Assert.False(deactivated.AktifMi);
    }

    private sealed class StubHksService : IHksService
    {
        private readonly IReadOnlyDictionary<int, IReadOnlyList<HksUrunCinsiDto>> _kindMap;

        public StubHksService(IReadOnlyDictionary<int, IReadOnlyList<HksUrunCinsiDto>> kindMap)
        {
            _kindMap = kindMap;
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
            => Task.FromResult<IReadOnlyList<HksSelectOptionDto>>([]);

        public Task<IReadOnlyList<HksUrunCinsiDto>> GetUrunCinsleriAsync(int urunId, CancellationToken cancellationToken = default)
            => Task.FromResult(_kindMap.TryGetValue(urunId, out var kinds) ? kinds : []);

        public Task<HksReferansKunyeResponseDto> GetReferansKunyelerAsync(HksReferansKunyeRequestDto request, CancellationToken cancellationToken = default)
            => Task.FromResult(new HksReferansKunyeResponseDto());

        public Task<HksReferansKunyeResponseDto> GetReferansKunyelerForTenantAsync(Guid tenantId, HksReferansKunyeRequestDto request, Func<HksSearchProgressDto, Task>? progressCallback = null, CancellationToken cancellationToken = default)
            => Task.FromResult(new HksReferansKunyeResponseDto());
    }

    private sealed class StubHksUrunService : IHksUrunService
    {
        public Task<IReadOnlyList<HksUrunKayitDto>> GetCurrentTenantProductsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<HksUrunKayitDto>>([]);

        public Task<IReadOnlyList<HksUrunKayitDto>> SyncCurrentTenantProductsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<HksUrunKayitDto>>([]);
    }
}
