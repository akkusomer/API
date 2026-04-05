using AtlasWeb.DTOs;
using AtlasWeb.Services;
using AtlasWeb.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace AtlasWeb.Tests;

public sealed class HksIlServiceTests
{
    [Fact]
    public async Task SyncCurrentTenantCitiesAsync_PersistsAndReactivatesGlobalCities()
    {
        await using var harness = new AtlasTestContext();
        var tenant = await harness.CreateTenantAsync("HKSIL1", "HKS Il");
        harness.SetUser(tenant.Id);

        harness.DbContext.HksIller.Add(new AtlasWeb.Models.HksIl
        {
            Id = IdGenerator.CreateV7(),
            HksIlId = 6,
            Ad = "ESKI ANKARA",
            AktifMi = false,
            SilinmeTarihi = DateTime.UtcNow,
            SilenKullanici = "test"
        });

        await harness.DbContext.SaveChangesAsync();

        var service = new HksIlService(
            harness.DbContext,
            harness.CurrentUser,
            new StubHksService([
                new HksSelectOptionDto { Id = 6, Ad = "ANKARA" },
                new HksSelectOptionDto { Id = 34, Ad = "ISTANBUL" }
            ]));

        var result = await service.SyncCurrentTenantCitiesAsync();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, item => item.HksIlId == 6 && item.Ad == "ANKARA");
        Assert.Contains(result, item => item.HksIlId == 34 && item.Ad == "ISTANBUL");

        var saved = await harness.DbContext.HksIller
            .IgnoreQueryFilters()
            .OrderBy(x => x.HksIlId)
            .ToListAsync();

        Assert.Equal(2, saved.Count);
        Assert.All(saved, item => Assert.True(item.AktifMi));
    }

    [Fact]
    public async Task SyncCurrentTenantCitiesAsync_DeactivatesCitiesMissingFromRemoteList()
    {
        await using var harness = new AtlasTestContext();
        var tenant = await harness.CreateTenantAsync("HKSIL2", "HKS Il 2");
        harness.SetUser(tenant.Id);

        harness.DbContext.HksIller.AddRange(
            new AtlasWeb.Models.HksIl
            {
                Id = IdGenerator.CreateV7(),
                HksIlId = 6,
                Ad = "ANKARA",
                AktifMi = true
            },
            new AtlasWeb.Models.HksIl
            {
                Id = IdGenerator.CreateV7(),
                HksIlId = 34,
                Ad = "ISTANBUL",
                AktifMi = true
            });

        await harness.DbContext.SaveChangesAsync();

        var service = new HksIlService(
            harness.DbContext,
            harness.CurrentUser,
            new StubHksService([
                new HksSelectOptionDto { Id = 6, Ad = "ANKARA" }
            ]));

        var result = await service.SyncCurrentTenantCitiesAsync();

        Assert.Single(result);
        Assert.Equal(6, result[0].HksIlId);

        var deactivated = await harness.DbContext.HksIller
            .IgnoreQueryFilters()
            .SingleAsync(x => x.HksIlId == 34);

        Assert.False(deactivated.AktifMi);
    }

    [Fact]
    public async Task SyncCitiesForAllTenantsAsync_UpdatesSharedCityDictionary()
    {
        await using var harness = new AtlasTestContext();
        harness.SetUser(AtlasWeb.Data.AtlasDbContext.SystemMusteriId, isAdmin: true, email: "admin@atlasweb.local");

        var sourceTenant = await harness.CreateTenantAsync("HKSIL3", "Kaynak Sirket");
        await harness.CreateTenantAsync("HKSIL4", "Hedef Sirket");

        harness.DbContext.HksIller.Add(new AtlasWeb.Models.HksIl
        {
            Id = IdGenerator.CreateV7(),
            HksIlId = 1,
            Ad = "ESKI KAYIT",
            AktifMi = true
        });

        await harness.DbContext.SaveChangesAsync();

        var service = new HksIlService(
            harness.DbContext,
            harness.CurrentUser,
            new StubHksService([
                new HksSelectOptionDto { Id = 6, Ad = "ANKARA" },
                new HksSelectOptionDto { Id = 34, Ad = "ISTANBUL" }
            ]));

        var result = await service.SyncCitiesForAllTenantsAsync(sourceTenant.Id);

        Assert.Equal(sourceTenant.Id, result.KaynakMusteriId);
        Assert.Equal(2, result.SirketSayisi);
        Assert.Equal(2, result.IlSayisi);

        var savedCities = await harness.DbContext.HksIller
            .IgnoreQueryFilters()
            .OrderBy(x => x.HksIlId)
            .ToListAsync();

        Assert.Equal(3, savedCities.Count);
        Assert.Contains(savedCities, item => item.HksIlId == 6 && item.AktifMi);
        Assert.Contains(savedCities, item => item.HksIlId == 34 && item.AktifMi);
        Assert.Contains(savedCities, item => item.HksIlId == 1 && !item.AktifMi);
    }

    private sealed class StubHksService : IHksService
    {
        private readonly IReadOnlyList<HksSelectOptionDto> _cities;

        public StubHksService(IReadOnlyList<HksSelectOptionDto> cities)
        {
            _cities = cities;
        }

        public Task<IReadOnlyList<HksSelectOptionDto>> GetSifatlarAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<HksSelectOptionDto>>([]);

        public Task<HksKayitliKisiSorguDto?> GetKayitliKisiSorguAsync(string tcKimlikVergiNo, CancellationToken cancellationToken = default)
            => Task.FromResult<HksKayitliKisiSorguDto?>(null);

        public Task<IReadOnlyList<HksSelectOptionDto>> GetIllerAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(_cities);

        public Task<IReadOnlyList<HksSelectOptionDto>> GetIllerForTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
            => Task.FromResult(_cities);

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
            => Task.FromResult<IReadOnlyList<HksUrunCinsiDto>>([]);

        public Task<HksReferansKunyeResponseDto> GetReferansKunyelerAsync(HksReferansKunyeRequestDto request, CancellationToken cancellationToken = default)
            => Task.FromResult(new HksReferansKunyeResponseDto());

        public Task<HksReferansKunyeResponseDto> GetReferansKunyelerForTenantAsync(Guid tenantId, HksReferansKunyeRequestDto request, Func<HksSearchProgressDto, Task>? progressCallback = null, CancellationToken cancellationToken = default)
            => Task.FromResult(new HksReferansKunyeResponseDto());
    }
}
