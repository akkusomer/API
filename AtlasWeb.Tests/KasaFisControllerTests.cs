using AtlasWeb.Controllers;
using AtlasWeb.DTOs;
using AtlasWeb.Models;
using AtlasWeb.Tests.Support;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AtlasWeb.Tests;

public class KasaFisControllerTests
{
    [Fact]
    public async Task Ekle_WhenCariBelongsToTenant_CreatesKasaFis()
    {
        await using var harness = new AtlasTestContext();
        var tenant = await harness.CreateTenantAsync("AKS11", "Kasa Tenant");
        var cariType = await harness.CreateCariTypeAsync(tenant.Id, "Musteri");
        var cari = await harness.CreateCariCardAsync(tenant.Id, cariType.Id, "Ornek Cari", "1111111111");
        harness.SetUser(tenant.Id);

        var controller = new KasaFisController(harness.DbContext, harness.CurrentUser);
        var result = await controller.Ekle(new KasaFisDto
        {
            KasaAdi = "MERKEZ TL KASA",
            BelgeKodu = "KF",
            IslemTipi = KasaIslemTipi.Tahsilat,
            CariKartId = cari.Id,
            Tarih = DateTime.SpecifyKind(new DateTime(2026, 4, 1), DateTimeKind.Utc),
            HareketTipi = "GENEL",
            Aciklama1 = "Cari tahsilati",
            Tutar = 400
        });

        var ok = Assert.IsType<OkObjectResult>(result);
        var fisId = ok.Value!.Read<Guid>("id");

        var fis = await harness.DbContext.KasaFisleri
            .IgnoreQueryFilters()
            .SingleAsync(x => x.Id == fisId);

        Assert.Equal(tenant.Id, fis.MusteriId);
        Assert.Equal(cari.Id, fis.CariKartId);
        Assert.Equal(400, fis.Tutar);
        Assert.Equal(1, fis.BelgeNo);
    }

    [Fact]
    public async Task Ekle_WhenCariBelongsToAnotherTenant_ReturnsBadRequest()
    {
        await using var harness = new AtlasTestContext();
        var tenantA = await harness.CreateTenantAsync("AKS12", "Tenant A");
        var tenantB = await harness.CreateTenantAsync("AKS13", "Tenant B");
        var cariType = await harness.CreateCariTypeAsync(tenantB.Id, "Musteri");
        var foreignCari = await harness.CreateCariCardAsync(tenantB.Id, cariType.Id, "Yabanci Cari", "2222222222");
        harness.SetUser(tenantA.Id);

        var controller = new KasaFisController(harness.DbContext, harness.CurrentUser);
        var result = await controller.Ekle(new KasaFisDto
        {
            KasaAdi = "MERKEZ TL KASA",
            BelgeKodu = "KF",
            IslemTipi = KasaIslemTipi.Tahsilat,
            CariKartId = foreignCari.Id,
            Tarih = DateTime.SpecifyKind(new DateTime(2026, 4, 1), DateTimeKind.Utc),
            HareketTipi = "GENEL",
            Tutar = 120
        });

        Assert.IsType<BadRequestObjectResult>(result);
    }
}
