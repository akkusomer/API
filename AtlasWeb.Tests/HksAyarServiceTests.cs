using AtlasWeb.Services;
using AtlasWeb.Tests.Support;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace AtlasWeb.Tests;

public sealed class HksAyarServiceTests
{
    [Fact]
    public async Task SaveCurrentTenantSettingsAsync_WhenTenantUser_PersistsEncryptedSecrets()
    {
        await using var harness = new AtlasTestContext();
        var tenant = await harness.CreateTenantAsync("AKSHKS", "HKS Test");
        harness.SetUser(tenant.Id);

        var service = CreateService(harness);

        var result = await service.SaveCurrentTenantSettingsAsync(new AtlasWeb.DTOs.HksAyarKaydetDto
        {
            KullaniciAdi = "aks-hks-user",
            Password = "aks-password",
            ServicePassword = "aks-service-password",
        });

        Assert.Equal("aks-hks-user", result.KullaniciAdi);
        Assert.True(result.HasPassword);
        Assert.True(result.HasServicePassword);

        var entity = await harness.DbContext.HksAyarlari
            .IgnoreQueryFilters()
            .SingleAsync(x => x.MusteriId == tenant.Id);

        Assert.Equal("aks-hks-user", entity.KullaniciAdi);
        Assert.NotEqual("aks-password", entity.PasswordCipherText);
        Assert.NotEqual("aks-service-password", entity.ServicePasswordCipherText);

        var credentials = await service.GetCurrentTenantCredentialsAsync();

        Assert.Equal("aks-hks-user", credentials.UserName);
        Assert.Equal("aks-password", credentials.Password);
        Assert.Equal("aks-service-password", credentials.ServicePassword);
    }

    [Fact]
    public async Task GetCurrentTenantCredentialsAsync_WhenSettingsMissing_ThrowsServiceUnavailable()
    {
        await using var harness = new AtlasTestContext();
        var tenant = await harness.CreateTenantAsync("AKSHKS2", "HKS Test 2");
        harness.SetUser(tenant.Id);

        var service = CreateService(harness);

        var exception = await Assert.ThrowsAsync<HksIntegrationException>(() => service.GetCurrentTenantCredentialsAsync());

        Assert.Equal(503, exception.StatusCode);
    }

    [Fact]
    public async Task GetTenantCredentialsAsync_WhenCalledOutsideTenantContext_ReadsSettingsWithIgnoreQueryFilters()
    {
        await using var harness = new AtlasTestContext();
        var tenant = await harness.CreateTenantAsync("AKSHKS3", "HKS Test 3");
        harness.SetUser(tenant.Id);

        var service = CreateService(harness);

        await service.SaveCurrentTenantSettingsAsync(new AtlasWeb.DTOs.HksAyarKaydetDto
        {
            KullaniciAdi = "aks-hks-worker",
            Password = "aks-password-worker",
            ServicePassword = "aks-service-password-worker",
        });

        harness.SetUser(null, isAdmin: false, email: "System_Automated_Job");

        var credentials = await service.GetTenantCredentialsAsync(tenant.Id);

        Assert.Equal("aks-hks-worker", credentials.UserName);
        Assert.Equal("aks-password-worker", credentials.Password);
        Assert.Equal("aks-service-password-worker", credentials.ServicePassword);
    }

    private static HksAyarService CreateService(AtlasTestContext harness)
    {
        return new HksAyarService(
            harness.DbContext,
            harness.CurrentUser,
            new EphemeralDataProtectionProvider(),
            new StubHostEnvironment(),
            NullLogger<HksAyarService>.Instance);
    }

    private sealed class StubHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Tests";
        public string ApplicationName { get; set; } = "AtlasWeb.Tests";
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
