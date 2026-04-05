using AtlasWeb.Controllers;
using AtlasWeb.DTOs;
using AtlasWeb.Tests.Support;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AtlasWeb.Tests;

public class FaturaControllerTests
{
    [Fact]
    public async Task Ekle_WhenReferencesBelongToTenant_CreatesInvoiceAndDetails()
    {
        await using var harness = new AtlasTestContext();
        var tenant = await harness.CreateTenantAsync("AKS07", "Fatura Test");
        var unit = await harness.CreateUnitAsync(tenant.Id, "Kilo", "KG");
        var stock = await harness.CreateStockAsync(tenant.Id, unit.Id, "00001", "Domates");
        var cariType = await harness.CreateCariTypeAsync(tenant.Id, "Musteri");
        var cariCard = await harness.CreateCariCardAsync(tenant.Id, cariType.Id, "Ornek Hal Ltd.", "1234567891");
        harness.SetUser(tenant.Id);

        var controller = new FaturaController(harness.DbContext, harness.CurrentUser);
        var result = await controller.Ekle(new FaturaDto
        {
            CariKartId = cariCard.Id,
            FaturaTarihi = DateTime.SpecifyKind(new DateTime(2026, 3, 30), DateTimeKind.Utc),
            Aciklama = "Test faturasi",
            TahsilEdilenTutar = 40,
            Kalemler =
            [
                new FaturaDetayDto
                {
                    StokId = stock.Id,
                    AlisKunye = "ALIS-KUNYE-001",
                    Miktar = 3,
                    BirimFiyat = 25,
                },
            ],
        });

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
        var payload = ok.Value!;

        var invoiceId = payload.Read<Guid>("id");

        var invoice = await harness.DbContext.Faturalar
            .IgnoreQueryFilters()
            .Include(x => x.Detaylar)
            .SingleAsync(x => x.Id == invoiceId);

        Assert.Equal(cariCard.Id, invoice.CariKartId);
        Assert.Equal("FTR000001", invoice.FaturaNo);
        Assert.Equal(40, invoice.TahsilEdilenTutar);
        Assert.Single(invoice.Detaylar);
        Assert.Equal(stock.Id, invoice.Detaylar.Single().StokId);
        Assert.Equal("ALIS-KUNYE-001", invoice.Detaylar.Single().AlisKunye);
    }

    [Fact]
    public async Task Ekle_WhenCariBelongsToAnotherTenant_ReturnsBadRequest()
    {
        await using var harness = new AtlasTestContext();
        var tenantA = await harness.CreateTenantAsync("AKS08", "Tenant A");
        var tenantB = await harness.CreateTenantAsync("AKS09", "Tenant B");

        var unit = await harness.CreateUnitAsync(tenantA.Id, "Adet", "ADT");
        var stock = await harness.CreateStockAsync(tenantA.Id, unit.Id, "00001", "Salatalik");
        var cariType = await harness.CreateCariTypeAsync(tenantB.Id, "Musteri");
        var foreignCari = await harness.CreateCariCardAsync(tenantB.Id, cariType.Id, "Yabanci Cari", "1234567892");
        harness.SetUser(tenantA.Id);

        var controller = new FaturaController(harness.DbContext, harness.CurrentUser);
        var result = await controller.Ekle(new FaturaDto
        {
            CariKartId = foreignCari.Id,
            FaturaTarihi = DateTime.SpecifyKind(new DateTime(2026, 3, 30), DateTimeKind.Utc),
            Kalemler =
            [
                new FaturaDetayDto
                {
                    StokId = stock.Id,
                    Miktar = 1,
                    BirimFiyat = 10,
                },
            ],
        });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Ekle_WhenTahsilatExceedsInvoiceTotal_ReturnsBadRequest()
    {
        await using var harness = new AtlasTestContext();
        var tenant = await harness.CreateTenantAsync("AKS10", "Tahsilat Test");
        var unit = await harness.CreateUnitAsync(tenant.Id, "Adet", "ADT");
        var stock = await harness.CreateStockAsync(tenant.Id, unit.Id, "00002", "Elma");
        var cariType = await harness.CreateCariTypeAsync(tenant.Id, "Musteri");
        var cariCard = await harness.CreateCariCardAsync(tenant.Id, cariType.Id, "Tahsilat Cari", "1234567893");
        harness.SetUser(tenant.Id);

        var controller = new FaturaController(harness.DbContext, harness.CurrentUser);
        var result = await controller.Ekle(new FaturaDto
        {
            CariKartId = cariCard.Id,
            FaturaTarihi = DateTime.SpecifyKind(new DateTime(2026, 3, 30), DateTimeKind.Utc),
            TahsilEdilenTutar = 250,
            Kalemler =
            [
                new FaturaDetayDto
                {
                    StokId = stock.Id,
                    Miktar = 2,
                    BirimFiyat = 100,
                },
            ],
        });

        Assert.IsType<BadRequestObjectResult>(result);
    }
}
