using AtlasWeb.Data;
using AtlasWeb.Models;
using AtlasWeb.Services;
using AtlasWeb.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace AtlasWeb.Tests;

public class TenantReferenceDataSeederTests
{
    [Fact]
    public async Task EnsureDefaultsForCustomerAsync_WhenDefaultCariTypeIsSoftDeleted_ReactivatesExistingRecord()
    {
        await using var harness = new AtlasTestContext();
        var tenant = await harness.CreateTenantAsync("AKS08", "Seeder CariTip");

        harness.DbContext.CariTipler.Add(new CariTip
        {
            Id = IdGenerator.CreateV7(),
            MusteriId = tenant.Id,
            Adi = "Musteri",
            Aciklama = "Varsayilan musteri cari tipi",
            AktifMi = false,
            SilinmeTarihi = DateTime.UtcNow,
            SilenKullanici = "test"
        });

        await harness.DbContext.SaveChangesAsync();

        await TenantReferenceDataSeeder.EnsureDefaultsForCustomerAsync(harness.DbContext, tenant.Id);

        var cariTypes = await harness.DbContext.CariTipler
            .IgnoreQueryFilters()
            .Where(x => x.MusteriId == tenant.Id && x.Adi == "Musteri")
            .ToListAsync();

        var cariType = Assert.Single(cariTypes);
        Assert.True(cariType.AktifMi);
        Assert.Null(cariType.SilinmeTarihi);
        Assert.Null(cariType.SilenKullanici);
    }

    [Fact]
    public async Task EnsureDefaultsForCustomerAsync_WhenDefaultUnitIsSoftDeleted_ReactivatesExistingRecord()
    {
        await using var harness = new AtlasTestContext();
        var tenant = await harness.CreateTenantAsync("AKS09", "Seeder Birim");

        harness.DbContext.Birimler.Add(new Birim
        {
            Id = IdGenerator.CreateV7(),
            MusteriId = tenant.Id,
            Ad = "Adet",
            Sembol = "ADET",
            AktifMi = false,
            SilinmeTarihi = DateTime.UtcNow,
            SilenKullanici = "test"
        });

        await harness.DbContext.SaveChangesAsync();

        await TenantReferenceDataSeeder.EnsureDefaultsForCustomerAsync(harness.DbContext, tenant.Id);

        var units = await harness.DbContext.Birimler
            .IgnoreQueryFilters()
            .Where(x => x.MusteriId == tenant.Id && x.Ad == "Adet")
            .ToListAsync();

        var unit = Assert.Single(units);
        Assert.True(unit.AktifMi);
        Assert.Null(unit.SilinmeTarihi);
        Assert.Null(unit.SilenKullanici);
    }
}
