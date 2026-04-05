using AtlasWeb.Controllers;
using AtlasWeb.Tests.Support;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AtlasWeb.Tests;

public sealed class MusteriControllerTests
{
    [Fact]
    public async Task SoftDelete_WhenSystemAdmin_CreatesAuditEntryAndDeactivatesTenant()
    {
        await using var harness = new AtlasTestContext();
        harness.SetUser(AtlasWeb.Data.AtlasDbContext.SystemMusteriId, isAdmin: true, email: "admin@atlasweb.local");

        var tenant = await harness.CreateTenantAsync("AKS12", "Musteri Sil");
        var controller = new MusteriController(harness.DbContext, harness.CurrentUser);

        var result = await controller.SoftDelete(tenant.Id);

        Assert.IsType<OkObjectResult>(result);

        var savedTenant = await harness.DbContext.Musteriler
            .IgnoreQueryFilters()
            .SingleAsync(x => x.Id == tenant.Id);

        Assert.False(savedTenant.AktifMi);

        var auditEntry = await harness.DbContext.AuditLogs
            .OrderByDescending(x => x.Timestamp)
            .FirstOrDefaultAsync(x => x.EntityName == "Musteri" && x.EntityId == tenant.Id.ToString() && x.Action == "Delete");

        Assert.NotNull(auditEntry);
        Assert.Equal("admin@atlasweb.local", auditEntry!.UserId);
    }
}
