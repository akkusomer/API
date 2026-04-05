using AtlasWeb.Controllers;
using AtlasWeb.DTOs;
using AtlasWeb.Tests.Support;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AtlasWeb.Tests;

public class StokControllerTests
{
    [Fact]
    public async Task Ekle_WhenUnitBelongsToTenant_GeneratesSequentialStockCode()
    {
        await using var harness = new AtlasTestContext();
        var tenant = await harness.CreateTenantAsync("AKS06", "Stok Test");
        var unit = await harness.CreateUnitAsync(tenant.Id, "Adet", "ADT");
        await harness.CreateStockAsync(tenant.Id, unit.Id, "00001", "İlk Stok");
        harness.SetUser(tenant.Id);

        var controller = new StokController(harness.DbContext, harness.CurrentUser);
        var result = await controller.Ekle(new StokDto
        {
            StokAdi = "İkinci Stok",
            YedekAdi = "Yedek İsim",
            BirimId = unit.Id,
        });

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
        var payload = ok.Value!;

        Assert.Equal("00002", payload.Read<string>("stokKodu"));
    }

    [Fact]
    public async Task Ekle_WhenHksSelectionsAreProvided_PersistsThem()
    {
        await using var harness = new AtlasTestContext();
        var tenant = await harness.CreateTenantAsync("AKS07", "HKS Stok");
        var unit = await harness.CreateUnitAsync(tenant.Id, "Kg", "KG");
        await harness.CreateHksUrunAsync(300, "DOMATES");
        await harness.CreateHksUretimSekliAsync(28, "Geleneksel");
        await harness.CreateHksUrunCinsiAsync(1166, 300, 28, "DOMATES", false);
        harness.SetUser(tenant.Id);

        var controller = new StokController(harness.DbContext, harness.CurrentUser);
        var result = await controller.Ekle(new StokDto
        {
            StokAdi = "Domates",
            BirimId = unit.Id,
            HksUrunId = 300,
            HksUretimSekliId = 28,
            HksUrunCinsiId = 1166,
            HksNitelik = "Yerli"
        });

        Assert.IsType<OkObjectResult>(result);

        var savedStock = await harness.DbContext.Stoklar.SingleAsync(x => x.StokAdi == "Domates");
        Assert.Equal(300, savedStock.HksUrunId);
        Assert.Equal(28, savedStock.HksUretimSekliId);
        Assert.Equal(1166, savedStock.HksUrunCinsiId);
        Assert.Equal("Yerli", savedStock.HksNitelik);
    }

    [Fact]
    public async Task Sil_WhenTenantUser_SoftDeletesWithAuditMetadata()
    {
        await using var harness = new AtlasTestContext();
        var tenant = await harness.CreateTenantAsync("AKS11", "Stok Sil");
        var unit = await harness.CreateUnitAsync(tenant.Id, "Adet", "ADT");
        var stock = await harness.CreateStockAsync(tenant.Id, unit.Id, "00001", "Silinecek Stok");
        harness.SetUser(tenant.Id, email: "operator@aks.local");

        var controller = new StokController(harness.DbContext, harness.CurrentUser);
        var result = await controller.Sil(stock.Id);

        Assert.IsType<OkObjectResult>(result);

        var savedStock = await harness.DbContext.Stoklar
            .IgnoreQueryFilters()
            .SingleAsync(x => x.Id == stock.Id);

        Assert.False(savedStock.AktifMi);
        Assert.Equal("operator@aks.local", savedStock.SilenKullanici);
        Assert.NotNull(savedStock.SilinmeTarihi);
    }

    [Fact]
    public async Task Guncelle_WhenHksSelectionsDoNotMatchProductKind_ReturnsBadRequest()
    {
        await using var harness = new AtlasTestContext();
        var tenant = await harness.CreateTenantAsync("AKS12", "Stok Guncelle");
        var unit = await harness.CreateUnitAsync(tenant.Id, "Adet", "ADT");
        var stock = await harness.CreateStockAsync(tenant.Id, unit.Id, "00001", "Domates");
        await harness.CreateHksUrunAsync(300, "DOMATES");
        await harness.CreateHksUretimSekliAsync(28, "Geleneksel");
        await harness.CreateHksUrunCinsiAsync(1166, 300, 28, "DOMATES", false);
        harness.SetUser(tenant.Id);

        var controller = new StokController(harness.DbContext, harness.CurrentUser);
        var result = await controller.Guncelle(stock.Id, new StokDto
        {
            StokAdi = "Domates",
            BirimId = unit.Id,
            HksUrunId = 300,
            HksUretimSekliId = 28,
            HksUrunCinsiId = 1166,
            HksNitelik = "İthal"
        });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
        Assert.Equal("Secilen HKS urun cinsi nitelik secimiyle eslesmiyor.", badRequest.Value!.Read<string>("hata"));
    }
}
