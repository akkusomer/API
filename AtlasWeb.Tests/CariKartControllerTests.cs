using AtlasWeb.Controllers;
using AtlasWeb.DTOs;
using AtlasWeb.Models;
using AtlasWeb.Tests.Support;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AtlasWeb.Tests;

public class CariKartControllerTests
{
    [Fact]
    public async Task Ekle_WhenCariTipBelongsToTenant_CreatesCariKart()
    {
        await using var harness = new AtlasTestContext();
        var tenant = await harness.CreateTenantAsync("AKS05", "Cari Test");
        var cariType = await harness.CreateCariTypeAsync(tenant.Id, "Musteri");
        await harness.CreateHksSifatAsync(5, "Komisyoncu");
        await harness.CreateHksIsletmeTuruAsync(18, "Bireysel Tuketim");
        await harness.CreateHksIlAsync(6, "ANKARA");
        await harness.CreateHksIlceAsync(62, 6, "CANKAYA");
        await harness.CreateHksBeldeAsync(6201, 62, "ORNEK BELDE");
        harness.SetUser(tenant.Id);

        var controller = new CariKartController(harness.DbContext, harness.CurrentUser);
        var result = await controller.Ekle(new CariKartDto
        {
            CariTipId = cariType.Id,
            Unvan = "Ornek Gida Ltd.",
            FaturaTipi = FaturaTipiEnum.Kurumsal,
            VTCK_No = "1234567890",
            Telefon = "02120000000",
            HksSifatId = 5,
            HksIsletmeTuruId = 18,
            HksHalIciIsyeriId = 901,
            HalIciIsyeriAdi = "Komisyoncu Dukkani",
            HksIlId = 6,
            HksIlceId = 62,
            HksBeldeId = 6201,
        });

        Assert.IsType<OkObjectResult>(result);

        var cariCard = await harness.DbContext.CariKartlar
            .IgnoreQueryFilters()
            .SingleAsync(x => x.MusteriId == tenant.Id && x.Unvan == "Ornek Gida Ltd.");

        Assert.Equal(cariType.Id, cariCard.CariTipId);
        Assert.Equal("1234567890", cariCard.VTCK_No);
        Assert.Equal(5, cariCard.HksSifatId);
        Assert.Equal(18, cariCard.HksIsletmeTuruId);
        Assert.Equal(901, cariCard.HksHalIciIsyeriId);
        Assert.Equal("Komisyoncu Dukkani", cariCard.HalIciIsyeriAdi);
        Assert.Equal(6, cariCard.HksIlId);
        Assert.Equal(62, cariCard.HksIlceId);
        Assert.Equal(6201, cariCard.HksBeldeId);
        Assert.Equal("ANKARA", cariCard.Il);
        Assert.Equal("CANKAYA", cariCard.Ilce);
        Assert.Equal("ORNEK BELDE", cariCard.Belde);
    }

    [Fact]
    public async Task Ekle_WhenVtckNoIsBlank_TreatsItAsNullAndAllowsMultipleRecords()
    {
        await using var harness = new AtlasTestContext();
        var tenant = await harness.CreateTenantAsync("AKS06", "Blank VTCK");
        var cariType = await harness.CreateCariTypeAsync(tenant.Id, "Musteri");
        harness.SetUser(tenant.Id);

        var controller = new CariKartController(harness.DbContext, harness.CurrentUser);

        var first = await controller.Ekle(new CariKartDto
        {
            CariTipId = cariType.Id,
            Unvan = "Birinci Cari",
            VTCK_No = "   ",
        });

        var second = await controller.Ekle(new CariKartDto
        {
            CariTipId = cariType.Id,
            Unvan = "Ikinci Cari",
            VTCK_No = "",
        });

        Assert.IsType<OkObjectResult>(first);
        Assert.IsType<OkObjectResult>(second);

        var saved = await harness.DbContext.CariKartlar
            .IgnoreQueryFilters()
            .Where(x => x.MusteriId == tenant.Id)
            .OrderBy(x => x.Unvan)
            .ToListAsync();

        Assert.Equal(2, saved.Count);
        Assert.All(saved, item => Assert.Null(item.VTCK_No));
    }

    [Fact]
    public async Task Ekle_WhenVtckNoAlreadyExists_ReturnsConflict()
    {
        await using var harness = new AtlasTestContext();
        var tenant = await harness.CreateTenantAsync("AKS07", "Duplicate VTCK");
        var cariType = await harness.CreateCariTypeAsync(tenant.Id, "Musteri");
        harness.SetUser(tenant.Id);

        await harness.CreateCariCardAsync(tenant.Id, cariType.Id, "Mevcut Cari", "1234567890");

        var controller = new CariKartController(harness.DbContext, harness.CurrentUser);
        var result = await controller.Ekle(new CariKartDto
        {
            CariTipId = cariType.Id,
            Unvan = "Yeni Cari",
            VTCK_No = "1234567890",
        });

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Contains("zaten", conflict.Value?.ToString());
    }

    [Fact]
    public async Task GetById_ReturnsFlatPayloadWithoutNavigationCycle()
    {
        await using var harness = new AtlasTestContext();
        var tenant = await harness.CreateTenantAsync("AKS08", "GetById Test");
        var cariType = await harness.CreateCariTypeAsync(tenant.Id, "Musteri");
        await harness.CreateHksSifatAsync(5, "Komisyoncu");
        await harness.CreateHksIsletmeTuruAsync(18, "Bireysel Tuketim");
        await harness.CreateHksIlAsync(6, "ANKARA");
        await harness.CreateHksIlceAsync(62, 6, "CANKAYA");
        await harness.CreateHksBeldeAsync(6201, 62, "ORNEK BELDE");
        var cariCard = await harness.CreateCariCardAsync(tenant.Id, cariType.Id, "Duz Cari", "5555555555");
        cariCard.AdiSoyadi = "Osman";
        cariCard.HksSifatId = 5;
        cariCard.HksIsletmeTuruId = 18;
        cariCard.HksHalIciIsyeriId = 901;
        cariCard.HalIciIsyeriAdi = "Komisyoncu Dukkani";
        cariCard.HksIlId = 6;
        cariCard.HksIlceId = 62;
        cariCard.HksBeldeId = 6201;
        cariCard.Il = "ANKARA";
        cariCard.Ilce = "CANKAYA";
        cariCard.Belde = "ORNEK BELDE";
        await harness.DbContext.SaveChangesAsync();
        harness.SetUser(tenant.Id);

        var controller = new CariKartController(harness.DbContext, harness.CurrentUser);
        var result = await controller.GetById(cariCard.Id);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = ok.Value!;

        Assert.Equal(cariCard.Id, payload.Read<Guid>("Id"));
        Assert.Equal("Osman", payload.Read<string>("AdiSoyadi"));
        Assert.Equal(5, payload.Read<int>("HksSifatId"));
        Assert.Equal("Komisyoncu", payload.Read<string>("Sifat"));
        Assert.Equal(18, payload.Read<int>("HksIsletmeTuruId"));
        Assert.Equal("Bireysel Tuketim", payload.Read<string>("IsletmeTuru"));
        Assert.Equal(901, payload.Read<int>("HksHalIciIsyeriId"));
        Assert.Equal("Komisyoncu Dukkani", payload.Read<string>("HalIciIsyeriAdi"));
        Assert.Equal(6, payload.Read<int>("HksIlId"));
        Assert.Equal(62, payload.Read<int>("HksIlceId"));
        Assert.Equal(6201, payload.Read<int>("HksBeldeId"));
        Assert.Equal("ANKARA", payload.Read<string>("Il"));
        Assert.Equal("CANKAYA", payload.Read<string>("Ilce"));
        Assert.Equal("ORNEK BELDE", payload.Read<string>("Belde"));
        Assert.Equal(cariType.Id, payload.Read<Guid>("CariTipId"));
        Assert.Equal("Musteri", payload.Read<string>("CariTipAdi"));
    }

    [Fact]
    public async Task GetEkstre_IncludesKasaFisMovementsInDebtCalculation()
    {
        await using var harness = new AtlasTestContext();
        var tenant = await harness.CreateTenantAsync("AKS14", "Ekstre Borc Test");
        var unit = await harness.CreateUnitAsync(tenant.Id, "Adet", "ADT");
        var stock = await harness.CreateStockAsync(tenant.Id, unit.Id, "00011", "Elma");
        var cariType = await harness.CreateCariTypeAsync(tenant.Id, "Musteri");
        var cariCard = await harness.CreateCariCardAsync(tenant.Id, cariType.Id, "Test Cari", "3333333333");
        harness.SetUser(tenant.Id);

        var invoice = await harness.CreateInvoiceAsync(tenant.Id, cariCard.Id, stock.Id, "FTR000111");
        invoice.Tutar = 100;
        invoice.TahsilEdilenTutar = 20;

        harness.DbContext.KasaFisleri.AddRange(
            new KasaFis
            {
                Id = Guid.NewGuid(),
                MusteriId = tenant.Id,
                KasaAdi = "MERKEZ TL KASA",
                BelgeKodu = "KF",
                BelgeNo = 1,
                IslemTipi = KasaIslemTipi.Tahsilat,
                CariKartId = cariCard.Id,
                Tarih = DateTime.SpecifyKind(new DateTime(2026, 4, 1), DateTimeKind.Utc),
                HareketTipi = "GENEL",
                Tutar = 30
            },
            new KasaFis
            {
                Id = Guid.NewGuid(),
                MusteriId = tenant.Id,
                KasaAdi = "MERKEZ TL KASA",
                BelgeKodu = "KF",
                BelgeNo = 2,
                IslemTipi = KasaIslemTipi.Odeme,
                CariKartId = cariCard.Id,
                Tarih = DateTime.SpecifyKind(new DateTime(2026, 4, 1), DateTimeKind.Utc),
                HareketTipi = "GENEL",
                Tutar = 10
            });

        await harness.DbContext.SaveChangesAsync();

        var controller = new CariKartController(harness.DbContext, harness.CurrentUser);
        var result = await controller.GetEkstre(cariCard.Id);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = ok.Value!;
        var ozet = payload.Read<object>("ozet")!;

        Assert.Equal(110m, ozet.Read<decimal>("ToplamBorc"));
        Assert.Equal(50m, ozet.Read<decimal>("ToplamTahsilat"));
        Assert.Equal(60m, ozet.Read<decimal>("KalanBorc"));
    }
}
