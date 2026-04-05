using AtlasWeb.DTOs;
using AtlasWeb.Services;
using AtlasWeb.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace AtlasWeb.Tests;

public sealed class HksIsletmeTuruServiceTests
{
    [Fact]
    public async Task SyncCurrentTenantBusinessTypesAsync_PersistsAndReactivatesSharedBusinessTypes()
    {
        await using var harness = new AtlasTestContext();
        var tenant = await harness.CreateTenantAsync("HKSIT1", "HKS Isletme Turu");
        harness.SetUser(tenant.Id);

        harness.DbContext.HksIsletmeTurleri.Add(new AtlasWeb.Models.HksIsletmeTuru
        {
            Id = IdGenerator.CreateV7(),
            HksIsletmeTuruId = 1,
            Ad = "ESKI TUR",
            AktifMi = false,
            SilinmeTarihi = DateTime.UtcNow,
            SilenKullanici = "test"
        });

        await harness.DbContext.SaveChangesAsync();

        var service = new HksIsletmeTuruService(
            harness.DbContext,
            harness.CurrentUser,
            new StubHksService(
            [
                new HksSelectOptionDto { Id = 1, Ad = "KOMISYONCU HALI" },
                new HksSelectOptionDto { Id = 2, Ad = "TUCCAR HALI" }
            ]));

        var result = await service.SyncCurrentTenantBusinessTypesAsync();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, item => item.HksIsletmeTuruId == 1 && item.Ad == "KOMISYONCU HALI");
        Assert.Contains(result, item => item.HksIsletmeTuruId == 2 && item.Ad == "TUCCAR HALI");

        var saved = await harness.DbContext.HksIsletmeTurleri
            .IgnoreQueryFilters()
            .OrderBy(x => x.HksIsletmeTuruId)
            .ToListAsync();

        Assert.Equal(2, saved.Count);
        Assert.All(saved, item => Assert.True(item.AktifMi));
    }

    [Fact]
    public async Task SyncCurrentTenantBusinessTypesAsync_DeactivatesMissingBusinessTypes()
    {
        await using var harness = new AtlasTestContext();
        var tenant = await harness.CreateTenantAsync("HKSIT2", "HKS Isletme Turu 2");
        harness.SetUser(tenant.Id);

        harness.DbContext.HksIsletmeTurleri.AddRange(
            new AtlasWeb.Models.HksIsletmeTuru
            {
                Id = IdGenerator.CreateV7(),
                HksIsletmeTuruId = 1,
                Ad = "KOMISYONCU HALI",
                AktifMi = true
            },
            new AtlasWeb.Models.HksIsletmeTuru
            {
                Id = IdGenerator.CreateV7(),
                HksIsletmeTuruId = 2,
                Ad = "TUCCAR HALI",
                AktifMi = true
            });

        await harness.DbContext.SaveChangesAsync();

        var service = new HksIsletmeTuruService(
            harness.DbContext,
            harness.CurrentUser,
            new StubHksService([new HksSelectOptionDto { Id = 1, Ad = "KOMISYONCU HALI" }]));

        var result = await service.SyncCurrentTenantBusinessTypesAsync();

        Assert.Single(result);
        Assert.Equal(1, result[0].HksIsletmeTuruId);

        var deactivated = await harness.DbContext.HksIsletmeTurleri
            .IgnoreQueryFilters()
            .SingleAsync(x => x.HksIsletmeTuruId == 2);

        Assert.False(deactivated.AktifMi);
    }

    private sealed class StubHksService : IHksService
    {
        private readonly IReadOnlyList<HksSelectOptionDto> _businessTypes;

        public StubHksService(IReadOnlyList<HksSelectOptionDto> businessTypes)
        {
            _businessTypes = businessTypes;
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
            => Task.FromResult(_businessTypes);

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
