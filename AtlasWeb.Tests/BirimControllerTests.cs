using AtlasWeb.Controllers;
using AtlasWeb.DTOs;
using AtlasWeb.Tests.Support;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AtlasWeb.Tests;

public class BirimControllerTests
{
    [Fact]
    public async Task Ekle_WhenTenantUser_CreatesUnit()
    {
        await using var harness = new AtlasTestContext();
        var tenant = await harness.CreateTenantAsync("AKS04", "Birim Test");
        harness.SetUser(tenant.Id);

        var controller = new BirimController(harness.DbContext, harness.CurrentUser);
        var result = await controller.Ekle(new BirimDto
        {
            Ad = "Koli",
            Sembol = "KOLI",
        });

        Assert.IsType<OkObjectResult>(result);

        var unit = await harness.DbContext.Birimler
            .IgnoreQueryFilters()
            .SingleAsync(x => x.MusteriId == tenant.Id && x.Ad == "Koli");

        Assert.Equal("KOLI", unit.Sembol);
    }
}
