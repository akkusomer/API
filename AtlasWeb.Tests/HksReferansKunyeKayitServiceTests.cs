using AtlasWeb.DTOs;
using AtlasWeb.Services;
using AtlasWeb.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AtlasWeb.Tests;

public sealed class HksReferansKunyeKayitServiceTests
{
    [Fact]
    public async Task SaveCurrentTenantSnapshotAsync_PersistsDatesAndItems()
    {
        await using var harness = new AtlasTestContext();
        var tenant = await harness.CreateTenantAsync("HKSKAYIT", "HKS Kayit");
        harness.SetUser(tenant.Id);

        var service = new HksReferansKunyeKayitService(
            harness.DbContext,
            harness.CurrentUser,
            NullLogger<HksReferansKunyeKayitService>.Instance);

        var baslangic = new DateTime(2026, 3, 1, 0, 0, 0);
        var bitis = new DateTime(2026, 3, 31, 23, 59, 59);

        var result = await service.SaveCurrentTenantSnapshotAsync(new HksReferansKunyeKaydetDto
        {
            BaslangicTarihi = baslangic,
            BitisTarihi = bitis,
            IslemKodu = "GTBWSRV0000001",
            Mesaj = "Basarili",
            ReferansKunyeler =
            [
                new HksReferansKunyeDto
                {
                    KunyeNo = 123456789,
                    MalinAdi = "DOMATES",
                    UniqueId = "UNIQUE-001"
                }
            ]
        });

        Assert.Equal(baslangic, result.BaslangicTarihi);
        Assert.Equal(bitis, result.BitisTarihi);
        Assert.Equal(1, result.KayitSayisi);
        Assert.Single(result.ReferansKunyeler);

        var entity = await harness.DbContext.HksReferansKunyeKayitlari
            .IgnoreQueryFilters()
            .SingleAsync(x => x.MusteriId == tenant.Id);

        Assert.Equal("GTBWSRV0000001", entity.IslemKodu);
        Assert.Equal(1, entity.KayitSayisi);
        Assert.Contains("DOMATES", entity.ReferansKunyelerJson);
    }

    [Fact]
    public async Task GetCurrentTenantSnapshotAsync_ReturnsLastSavedSnapshot()
    {
        await using var harness = new AtlasTestContext();
        var tenant = await harness.CreateTenantAsync("HKSKAYIT2", "HKS Kayit 2");
        harness.SetUser(tenant.Id);

        var service = new HksReferansKunyeKayitService(
            harness.DbContext,
            harness.CurrentUser,
            NullLogger<HksReferansKunyeKayitService>.Instance);

        await service.SaveCurrentTenantSnapshotAsync(new HksReferansKunyeKaydetDto
        {
            BaslangicTarihi = new DateTime(2026, 2, 1, 0, 0, 0),
            BitisTarihi = new DateTime(2026, 2, 28, 23, 59, 59),
            IslemKodu = "GTBWSRV0000001",
            ReferansKunyeler =
            [
                new HksReferansKunyeDto
                {
                    KunyeNo = 987654321,
                    MalinAdi = "BIBER",
                    UniqueId = "UNIQUE-002"
                }
            ]
        });

        var snapshot = await service.GetCurrentTenantSnapshotAsync();

        Assert.NotNull(snapshot);
        Assert.Equal(1, snapshot.KayitSayisi);
        Assert.Single(snapshot.ReferansKunyeler);
        Assert.Equal("BIBER", snapshot.ReferansKunyeler[0].MalinAdi);
    }

    [Fact]
    public async Task SaveCurrentTenantSnapshotAsync_WhenOnlyDatesProvided_PreservesExistingItems()
    {
        await using var harness = new AtlasTestContext();
        var tenant = await harness.CreateTenantAsync("HKSKAYIT3", "HKS Kayit 3");
        harness.SetUser(tenant.Id);

        var service = new HksReferansKunyeKayitService(
            harness.DbContext,
            harness.CurrentUser,
            NullLogger<HksReferansKunyeKayitService>.Instance);

        await service.SaveCurrentTenantSnapshotAsync(new HksReferansKunyeKaydetDto
        {
            BaslangicTarihi = new DateTime(2026, 1, 1, 0, 0, 0),
            BitisTarihi = new DateTime(2026, 1, 31, 23, 59, 59),
            IslemKodu = "GTBWSRV0000001",
            ReferansKunyeler =
            [
                new HksReferansKunyeDto
                {
                    KunyeNo = 111111111,
                    MalinAdi = "SALATALIK",
                    UniqueId = "UNIQUE-111"
                }
            ]
        });

        var updated = await service.SaveCurrentTenantSnapshotAsync(new HksReferansKunyeKaydetDto
        {
            BaslangicTarihi = new DateTime(2026, 2, 1, 0, 0, 0),
            BitisTarihi = new DateTime(2026, 2, 28, 23, 59, 59),
        });

        Assert.Equal(1, updated.KayitSayisi);
        Assert.Single(updated.ReferansKunyeler);
        Assert.Equal("SALATALIK", updated.ReferansKunyeler[0].MalinAdi);
        Assert.Equal(new DateTime(2026, 2, 1, 0, 0, 0), updated.BaslangicTarihi);
        Assert.Equal(new DateTime(2026, 2, 28, 23, 59, 59), updated.BitisTarihi);
    }

    [Fact]
    public async Task QueueCurrentTenantSearchAsync_WhenJobAlreadyRunning_ThrowsConflict()
    {
        await using var harness = new AtlasTestContext();
        var tenant = await harness.CreateTenantAsync("HKSKUYRUK", "HKS Kuyruk");
        harness.SetUser(tenant.Id);

        var service = new HksReferansKunyeKayitService(
            harness.DbContext,
            harness.CurrentUser,
            NullLogger<HksReferansKunyeKayitService>.Instance);

        await service.QueueCurrentTenantSearchAsync(new HksReferansKunyeRequestDto
        {
            BaslangicTarihi = new DateTime(2026, 3, 1, 0, 0, 0),
            BitisTarihi = new DateTime(2026, 3, 31, 23, 59, 59),
            KalanMiktariSifirdanBuyukOlanlar = true
        });

        var exception = await Assert.ThrowsAsync<HksIntegrationException>(() => service.QueueCurrentTenantSearchAsync(new HksReferansKunyeRequestDto
        {
            BaslangicTarihi = new DateTime(2026, 2, 1, 0, 0, 0),
            BitisTarihi = new DateTime(2026, 2, 28, 23, 59, 59),
            KalanMiktariSifirdanBuyukOlanlar = true
        }));

        Assert.Equal(409, exception.StatusCode);
    }
}
