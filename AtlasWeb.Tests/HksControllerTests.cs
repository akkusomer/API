using AtlasWeb.Controllers;
using AtlasWeb.DTOs;
using AtlasWeb.Services;
using AtlasWeb.Tests.Support;
using Microsoft.AspNetCore.Mvc;

namespace AtlasWeb.Tests;

public sealed class HksControllerTests
{
    [Fact]
    public async Task GetReferansKunyeler_WhenUrunIdMissing_DoesNotReturnBadRequest()
    {
        var currentUser = new TestCurrentUserService
        {
            MusteriId = Guid.NewGuid(),
            IsAdmin = false,
            EPosta = "user@atlasweb.local",
        };

        var controller = new HksController(
            new StubHksService(),
            new StubHksAyarService(),
            new StubHksSifatService(),
            new StubHksIlService(),
            new StubHksIlceService(),
            new StubHksBeldeService(),
            new StubHksUrunService(),
            new StubHksUrunBirimService(),
            new StubHksIsletmeTuruService(),
            new StubHksUretimSekliService(),
            new StubHksUrunCinsiService(),
            new StubHksReferansKunyeKayitService(),
            currentUser);

        var result = await controller.GetReferansKunyeler(
            new HksReferansKunyeRequestDto
            {
                BaslangicTarihi = new DateTime(2026, 3, 1),
                BitisTarihi = new DateTime(2026, 3, 30),
                KalanMiktariSifirdanBuyukOlanlar = true,
            },
            CancellationToken.None);

        Assert.IsType<AcceptedResult>(result);
    }

    [Fact]
    public async Task GetReferansKunyeler_WhenDateRangeExceedsOneMonth_DoesNotReturnBadRequest()
    {
        var currentUser = new TestCurrentUserService
        {
            MusteriId = Guid.NewGuid(),
            IsAdmin = false,
            EPosta = "user@atlasweb.local",
        };

        var controller = new HksController(
            new StubHksService(),
            new StubHksAyarService(),
            new StubHksSifatService(),
            new StubHksIlService(),
            new StubHksIlceService(),
            new StubHksBeldeService(),
            new StubHksUrunService(),
            new StubHksUrunBirimService(),
            new StubHksIsletmeTuruService(),
            new StubHksUretimSekliService(),
            new StubHksUrunCinsiService(),
            new StubHksReferansKunyeKayitService(),
            currentUser);

        var result = await controller.GetReferansKunyeler(
            new HksReferansKunyeRequestDto
            {
                BaslangicTarihi = new DateTime(2026, 2, 28),
                BitisTarihi = new DateTime(2026, 3, 30),
                UrunId = 300,
                KalanMiktariSifirdanBuyukOlanlar = true,
            },
            CancellationToken.None);

        Assert.IsType<AcceptedResult>(result);
    }

    [Fact]
    public async Task GetKayitliKisiSorgu_WhenServiceReturnsMatchedSifatIds_MapsToSifatNames()
    {
        var currentUser = new TestCurrentUserService
        {
            MusteriId = Guid.NewGuid(),
            IsAdmin = false,
            EPosta = "user@atlasweb.local",
        };

        var controller = new HksController(
            new StubHksService
            {
                KayitliKisiSorguResult = new HksKayitliKisiSorguDto
                {
                    TcKimlikVergiNo = "10163499474",
                    KayitliKisiMi = true,
                    SifatIds = [4, 7]
                },
                HalIciIsyeriResults =
                [
                    new HksHalIciIsyeriDto
                    {
                        Id = 901,
                        HalId = 6,
                        HalAdi = "ANKARA HALI",
                        Ad = "Komisyoncu Dukkani",
                        TcKimlikVergiNo = "10163499474"
                    }
                ]
            },
            new StubHksAyarService(),
            new StubHksSifatService
            {
                Results =
                [
                    new HksSifatKayitDto { HksSifatId = 4, Ad = "Komisyoncu" },
                    new HksSifatKayitDto { HksSifatId = 7, Ad = "Market" },
                ]
            },
            new StubHksIlService(),
            new StubHksIlceService(),
            new StubHksBeldeService(),
            new StubHksUrunService(),
            new StubHksUrunBirimService(),
            new StubHksIsletmeTuruService(),
            new StubHksUretimSekliService(),
            new StubHksUrunCinsiService(),
            new StubHksReferansKunyeKayitService(),
            currentUser);

        var result = await controller.GetKayitliKisiSorgu("10163499474", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<HksKayitliKisiSifatSonucDto>(ok.Value);
        Assert.True(payload.KayitliKisiMi);
        Assert.Equal(2, payload.Sifatlar.Count);
        Assert.Contains(payload.Sifatlar, item => item.Id == 4 && item.Ad == "Komisyoncu");
        Assert.Contains(payload.Sifatlar, item => item.Id == 7 && item.Ad == "Market");
        Assert.Single(payload.HalIciIsyerleri);
        Assert.Contains(payload.HalIciIsyerleri, item => item.Id == 901 && item.Ad == "Komisyoncu Dukkani" && item.HalAdi == "ANKARA HALI");
    }

    private sealed class StubHksService : IHksService
    {
        public HksKayitliKisiSorguDto? KayitliKisiSorguResult { get; set; }
        public IReadOnlyList<HksHalIciIsyeriDto> HalIciIsyeriResults { get; set; } = [];

        public Task<IReadOnlyList<HksSelectOptionDto>> GetSifatlarAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<HksSelectOptionDto>>([]);

        public Task<HksKayitliKisiSorguDto?> GetKayitliKisiSorguAsync(string tcKimlikVergiNo, CancellationToken cancellationToken = default)
            => Task.FromResult(KayitliKisiSorguResult);

        public Task<IReadOnlyList<HksHalIciIsyeriDto>> GetHalIciIsyerleriAsync(string tcKimlikVergiNo, CancellationToken cancellationToken = default)
            => Task.FromResult(HalIciIsyeriResults);

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

    private sealed class StubHksAyarService : IHksAyarService
    {
        public Task<HksAyarDto> GetTenantSettingsAsync(Guid tenantId, CancellationToken cancellationToken = default)
            => Task.FromResult(new HksAyarDto());

        public Task<HksAyarDto> GetCurrentTenantSettingsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new HksAyarDto());

        public Task<HksAyarDto> SaveCurrentTenantSettingsAsync(HksAyarKaydetDto request, CancellationToken cancellationToken = default)
            => Task.FromResult(new HksAyarDto());

        public Task<HksCredentialSet> GetCurrentTenantCredentialsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new HksCredentialSet());

        public Task<HksCredentialSet> GetTenantCredentialsAsync(Guid tenantId, CancellationToken cancellationToken = default)
            => Task.FromResult(new HksCredentialSet());
    }

    private sealed class StubHksUrunService : IHksUrunService
    {
        public Task<IReadOnlyList<HksUrunKayitDto>> GetCurrentTenantProductsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<HksUrunKayitDto>>([]);

        public Task<IReadOnlyList<HksUrunKayitDto>> SyncCurrentTenantProductsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<HksUrunKayitDto>>([]);
    }

    private sealed class StubHksSifatService : IHksSifatService
    {
        public IReadOnlyList<HksSifatKayitDto> Results { get; set; } = [];

        public Task<IReadOnlyList<HksSifatKayitDto>> GetCurrentTenantSifatlarAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Results);

        public Task<IReadOnlyList<HksSifatKayitDto>> SyncCurrentTenantSifatlarAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Results);
    }

    private sealed class StubHksUrunBirimService : IHksUrunBirimService
    {
        public Task<IReadOnlyList<HksUrunBirimKayitDto>> GetCurrentTenantProductUnitsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<HksUrunBirimKayitDto>>([]);

        public Task<IReadOnlyList<HksUrunBirimKayitDto>> SyncCurrentTenantProductUnitsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<HksUrunBirimKayitDto>>([]);
    }

    private sealed class StubHksIsletmeTuruService : IHksIsletmeTuruService
    {
        public Task<IReadOnlyList<HksIsletmeTuruKayitDto>> GetCurrentTenantBusinessTypesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<HksIsletmeTuruKayitDto>>([]);

        public Task<IReadOnlyList<HksIsletmeTuruKayitDto>> SyncCurrentTenantBusinessTypesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<HksIsletmeTuruKayitDto>>([]);
    }

    private sealed class StubHksUretimSekliService : IHksUretimSekliService
    {
        public Task<IReadOnlyList<HksUretimSekliKayitDto>> GetCurrentTenantProductionShapesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<HksUretimSekliKayitDto>>([]);

        public Task<IReadOnlyList<HksUretimSekliKayitDto>> SyncCurrentTenantProductionShapesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<HksUretimSekliKayitDto>>([]);
    }

    private sealed class StubHksUrunCinsiService : IHksUrunCinsiService
    {
        public Task<IReadOnlyList<HksUrunCinsiKayitDto>> GetCurrentTenantProductKindsAsync(int? urunId = null, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<HksUrunCinsiKayitDto>>([]);

        public Task<IReadOnlyList<HksUrunCinsiKayitDto>> SyncCurrentTenantProductKindsAsync(int? urunId = null, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<HksUrunCinsiKayitDto>>([]);
    }

    private sealed class StubHksIlService : IHksIlService
    {
        public Task<IReadOnlyList<HksIlKayitDto>> GetCurrentTenantCitiesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<HksIlKayitDto>>([]);

        public Task<IReadOnlyList<HksIlKayitDto>> SyncCurrentTenantCitiesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<HksIlKayitDto>>([]);

        public Task<HksIlTopluSenkronSonucDto> SyncCitiesForAllTenantsAsync(Guid sourceTenantId, CancellationToken cancellationToken = default)
            => Task.FromResult(new HksIlTopluSenkronSonucDto());
    }

    private sealed class StubHksIlceService : IHksIlceService
    {
        public Task<IReadOnlyList<HksIlceKayitDto>> GetCurrentTenantDistrictsAsync(int? ilId = null, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<HksIlceKayitDto>>([]);

        public Task<IReadOnlyList<HksIlceKayitDto>> SyncCurrentTenantDistrictsAsync(int? ilId = null, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<HksIlceKayitDto>>([]);

        public Task<HksIlceTopluSenkronSonucDto> SyncDistrictsForAllTenantsAsync(Guid sourceTenantId, CancellationToken cancellationToken = default)
            => Task.FromResult(new HksIlceTopluSenkronSonucDto());
    }

    private sealed class StubHksBeldeService : IHksBeldeService
    {
        public Task<IReadOnlyList<HksBeldeKayitDto>> GetCurrentTenantTownsAsync(int? ilceId = null, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<HksBeldeKayitDto>>([]);

        public Task<IReadOnlyList<HksBeldeKayitDto>> SyncCurrentTenantTownsAsync(int? ilceId = null, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<HksBeldeKayitDto>>([]);

        public Task<HksBeldeTopluSenkronSonucDto> SyncTownsForAllTenantsAsync(Guid sourceTenantId, CancellationToken cancellationToken = default)
            => Task.FromResult(new HksBeldeTopluSenkronSonucDto());
    }

    private sealed class StubHksReferansKunyeKayitService : IHksReferansKunyeKayitService
    {
        public Task<HksReferansKunyeKayitDto?> GetCurrentTenantSnapshotAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<HksReferansKunyeKayitDto?>(null);

        public Task<HksReferansKunyeKayitDto> QueueCurrentTenantSearchAsync(HksReferansKunyeRequestDto request, CancellationToken cancellationToken = default)
            => Task.FromResult(new HksReferansKunyeKayitDto
            {
                Durum = HksReferansKunyeDurum.Kuyrukta,
                ProgressPercent = 0,
                ProgressLabel = "HKS sorgusu kuyruga alindi"
            });

        public Task<HksReferansKunyeKayitDto> SaveCurrentTenantSnapshotAsync(HksReferansKunyeKaydetDto request, CancellationToken cancellationToken = default)
            => Task.FromResult(new HksReferansKunyeKayitDto());
    }
}
