using AtlasWeb.Controllers;
using AtlasWeb.DTOs;
using AtlasWeb.Models;
using AtlasWeb.Services;
using AtlasWeb.Tests.Support;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AtlasWeb.Tests;

public class AdminControllerTests
{
    [Fact]
    public async Task AddUserToMusteri_WhenSystemAdmin_CreatesTenantUser()
    {
        await using var harness = new AtlasTestContext();
        harness.SetUser(AtlasWeb.Data.AtlasDbContext.SystemMusteriId, isAdmin: true);

        var tenant = await harness.CreateTenantAsync("AKS02", "Akkuş Müşteri");
        var controller = new AdminController(
            harness.DbContext,
            harness.CurrentUser,
            new StubHksIlService(),
            new StubHksIlceService(),
            new StubHksBeldeService());

        var result = await controller.AddUserToMusteri(tenant.Id, new RegisterUserDto
        {
            Ad = "Ayşe",
            Soyad = "Yılmaz",
            EPosta = "ayse@aks.local",
            Telefon = "05550000000",
            Sifre = "Strong!123",
        });

        Assert.IsType<OkObjectResult>(result);

        var createdUser = await harness.DbContext.Kullanicilar
            .IgnoreQueryFilters()
            .SingleAsync(x => x.EPosta == "ayse@aks.local");

        Assert.Equal(tenant.Id, createdUser.MusteriId);
        Assert.Equal(KullaniciRol.User, createdUser.Rol);
        Assert.Equal("Ayşe", createdUser.Ad);
    }

    [Fact]
    public async Task GetYoneticiler_WhenNotSystemAdmin_ReturnsForbid()
    {
        await using var harness = new AtlasTestContext();
        var tenant = await harness.CreateTenantAsync("AKS03", "Yetki Test");
        harness.SetUser(tenant.Id, isAdmin: false);

        var controller = new AdminController(
            harness.DbContext,
            harness.CurrentUser,
            new StubHksIlService(),
            new StubHksIlceService(),
            new StubHksBeldeService());
        var result = await controller.GetYoneticiler();

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task DeleteUser_WhenSystemAdmin_SoftDeletesWithAuditMetadata()
    {
        await using var harness = new AtlasTestContext();
        harness.SetUser(AtlasWeb.Data.AtlasDbContext.SystemMusteriId, isAdmin: true, email: "admin@atlasweb.local");

        var tenant = await harness.CreateTenantAsync("AKS10", "Delete User");
        var user = await harness.CreateUserAsync(tenant.Id, "delete-user@aks.local");
        var controller = new AdminController(
            harness.DbContext,
            harness.CurrentUser,
            new StubHksIlService(),
            new StubHksIlceService(),
            new StubHksBeldeService());

        var result = await controller.DeleteUser(user.Id);

        Assert.IsType<OkObjectResult>(result);

        var savedUser = await harness.DbContext.Kullanicilar
            .IgnoreQueryFilters()
            .SingleAsync(x => x.Id == user.Id);

        Assert.False(savedUser.AktifMi);
        Assert.Equal("admin@atlasweb.local", savedUser.SilenKullanici);
        Assert.NotNull(savedUser.SilinmeTarihi);
    }

    [Fact]
    public async Task SyncHksCitiesForAllCustomers_WhenSystemAdmin_ReturnsOk()
    {
        await using var harness = new AtlasTestContext();
        harness.SetUser(AtlasWeb.Data.AtlasDbContext.SystemMusteriId, isAdmin: true);

        var tenant = await harness.CreateTenantAsync("AKS11", "HKS Kaynak Sirket");
        var controller = new AdminController(
            harness.DbContext,
            harness.CurrentUser,
            new StubHksIlService(),
            new StubHksIlceService(),
            new StubHksBeldeService());

        var result = await controller.SyncHksCitiesForAllCustomers(tenant.Id, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<HksIlTopluSenkronSonucDto>(okResult.Value);

        Assert.Equal(tenant.Id, payload.KaynakMusteriId);
        Assert.Equal(3, payload.SirketSayisi);
        Assert.Equal(81, payload.IlSayisi);
    }

    private sealed class StubHksIlService : IHksIlService
    {
        public Task<IReadOnlyList<HksIlKayitDto>> GetCurrentTenantCitiesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<HksIlKayitDto>>([]);

        public Task<IReadOnlyList<HksIlKayitDto>> SyncCurrentTenantCitiesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<HksIlKayitDto>>([]);

        public Task<HksIlTopluSenkronSonucDto> SyncCitiesForAllTenantsAsync(Guid sourceTenantId, CancellationToken cancellationToken = default)
            => Task.FromResult(new HksIlTopluSenkronSonucDto
            {
                KaynakMusteriId = sourceTenantId,
                SirketSayisi = 3,
                IlSayisi = 81,
                GuncellemeTarihi = DateTime.UtcNow
            });
    }

    private sealed class StubHksIlceService : IHksIlceService
    {
        public Task<IReadOnlyList<HksIlceKayitDto>> GetCurrentTenantDistrictsAsync(int? ilId = null, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<HksIlceKayitDto>>([]);

        public Task<IReadOnlyList<HksIlceKayitDto>> SyncCurrentTenantDistrictsAsync(int? ilId = null, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<HksIlceKayitDto>>([]);

        public Task<HksIlceTopluSenkronSonucDto> SyncDistrictsForAllTenantsAsync(Guid sourceTenantId, CancellationToken cancellationToken = default)
            => Task.FromResult(new HksIlceTopluSenkronSonucDto
            {
                KaynakMusteriId = sourceTenantId,
                SirketSayisi = 3,
                IlceSayisi = 973,
                GuncellemeTarihi = DateTime.UtcNow
            });
    }

    private sealed class StubHksBeldeService : IHksBeldeService
    {
        public Task<IReadOnlyList<HksBeldeKayitDto>> GetCurrentTenantTownsAsync(int? ilceId = null, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<HksBeldeKayitDto>>([]);

        public Task<IReadOnlyList<HksBeldeKayitDto>> SyncCurrentTenantTownsAsync(int? ilceId = null, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<HksBeldeKayitDto>>([]);

        public Task<HksBeldeTopluSenkronSonucDto> SyncTownsForAllTenantsAsync(Guid sourceTenantId, CancellationToken cancellationToken = default)
            => Task.FromResult(new HksBeldeTopluSenkronSonucDto
            {
                KaynakMusteriId = sourceTenantId,
                SirketSayisi = 3,
                BeldeSayisi = 390,
                GuncellemeTarihi = DateTime.UtcNow
            });
    }
}
