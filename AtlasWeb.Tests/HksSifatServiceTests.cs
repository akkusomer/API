using AtlasWeb.DTOs;
using AtlasWeb.Services;
using AtlasWeb.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace AtlasWeb.Tests;

public sealed class HksSifatServiceTests
{
    [Fact]
    public async Task SyncCurrentTenantSifatlarAsync_PersistsAndReactivatesSharedSifatlar()
    {
        await using var harness = new AtlasTestContext();
        var tenant = await harness.CreateTenantAsync("HKSSF1", "HKS Sifat");
        harness.SetUser(tenant.Id);

        harness.DbContext.HksSifatlar.Add(new AtlasWeb.Models.HksSifat
        {
            Id = IdGenerator.CreateV7(),
            HksSifatId = 1,
            Ad = "ESKI SIFAT",
            AktifMi = false,
            SilinmeTarihi = DateTime.UtcNow,
            SilenKullanici = "test"
        });

        await harness.DbContext.SaveChangesAsync();

        var service = new HksSifatService(
            harness.DbContext,
            harness.CurrentUser,
            new StubHksService(
            [
                new HksSelectOptionDto { Id = 1, Ad = "URETICI" },
                new HksSelectOptionDto { Id = 2, Ad = "TUCCAR" }
            ]));

        var result = await service.SyncCurrentTenantSifatlarAsync();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, item => item.HksSifatId == 1 && item.Ad == "URETICI");
        Assert.Contains(result, item => item.HksSifatId == 2 && item.Ad == "TUCCAR");

        var saved = await harness.DbContext.HksSifatlar
            .IgnoreQueryFilters()
            .OrderBy(x => x.HksSifatId)
            .ToListAsync();

        Assert.Equal(2, saved.Count);
        Assert.All(saved, item => Assert.True(item.AktifMi));
    }

    [Fact]
    public async Task SyncCurrentTenantSifatlarAsync_DeactivatesMissingSifatlar()
    {
        await using var harness = new AtlasTestContext();
        var tenant = await harness.CreateTenantAsync("HKSSF2", "HKS Sifat 2");
        harness.SetUser(tenant.Id);

        harness.DbContext.HksSifatlar.AddRange(
            new AtlasWeb.Models.HksSifat
            {
                Id = IdGenerator.CreateV7(),
                HksSifatId = 1,
                Ad = "URETICI",
                AktifMi = true
            },
            new AtlasWeb.Models.HksSifat
            {
                Id = IdGenerator.CreateV7(),
                HksSifatId = 2,
                Ad = "TUCCAR",
                AktifMi = true
            });

        await harness.DbContext.SaveChangesAsync();

        var service = new HksSifatService(
            harness.DbContext,
            harness.CurrentUser,
            new StubHksService([new HksSelectOptionDto { Id = 1, Ad = "URETICI" }]));

        var result = await service.SyncCurrentTenantSifatlarAsync();

        Assert.Single(result);
        Assert.Equal(1, result[0].HksSifatId);

        var deactivated = await harness.DbContext.HksSifatlar
            .IgnoreQueryFilters()
            .SingleAsync(x => x.HksSifatId == 2);

        Assert.False(deactivated.AktifMi);
    }

    private sealed class StubHksService : IHksService
    {
        private readonly IReadOnlyList<HksSelectOptionDto> _sifatlar;

        public StubHksService(IReadOnlyList<HksSelectOptionDto> sifatlar)
        {
            _sifatlar = sifatlar;
        }

        public Task<IReadOnlyList<HksSelectOptionDto>> GetSifatlarAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(_sifatlar);

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
            => Task.FromResult<IReadOnlyList<HksUrunCinsiDto>>([]);

        public Task<HksReferansKunyeResponseDto> GetReferansKunyelerAsync(HksReferansKunyeRequestDto request, CancellationToken cancellationToken = default)
            => Task.FromResult(new HksReferansKunyeResponseDto());

        public Task<HksReferansKunyeResponseDto> GetReferansKunyelerForTenantAsync(Guid tenantId, HksReferansKunyeRequestDto request, Func<HksSearchProgressDto, Task>? progressCallback = null, CancellationToken cancellationToken = default)
            => Task.FromResult(new HksReferansKunyeResponseDto());
    }
}
