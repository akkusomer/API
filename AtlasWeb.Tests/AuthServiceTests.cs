using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AtlasWeb.DTOs;
using AtlasWeb.Services;
using AtlasWeb.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace AtlasWeb.Tests;

public class AuthServiceTests
{
    private static IConfiguration BuildAuthConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "AtlasWebSuperSecretJwtKey_2026_Testing",
                ["Jwt:Issuer"] = "atlasweb-tests",
                ["Jwt:Audience"] = "atlasweb-users",
            })
            .Build();
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsTokensAndStoresRefreshToken()
    {
        await using var harness = new AtlasTestContext();
        var tenant = await harness.CreateTenantAsync("AKS01", "Akkus Test");
        await harness.CreateUserAsync(tenant.Id, "operator@atlasweb.local", password: "Strong!123", ad: "Omer", soyad: "Akkus");

        var service = new AuthService(
            harness.DbContext,
            BuildAuthConfiguration(),
            NullLogger<AuthService>.Instance,
            new TestEmailSender());

        var result = await service.LoginAsync(
            new LoginDto { EPosta = "operator@atlasweb.local", Sifre = "Strong!123" },
            "127.0.0.1");

        Assert.Equal(AuthStatus.Success, result.Status);
        Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(result.RefreshToken));

        var tokenCount = await harness.DbContext.KullaniciTokenler
            .IgnoreQueryFilters()
            .CountAsync();

        Assert.Equal(1, tokenCount);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.AccessToken);
        Assert.Equal("Omer Akkus", token.Claims.First(x => x.Type == ClaimTypes.Name).Value);
        Assert.Equal("Akkus Test", token.Claims.First(x => x.Type == "CompanyName").Value);
        Assert.Equal("Akkus Test", token.Claims.First(x => x.Type == "MusteriUnvan").Value);
    }

    [Fact]
    public async Task LogoutByRefreshTokenAsync_WithValidRefreshToken_RemovesMatchingSession()
    {
        await using var harness = new AtlasTestContext();
        var tenant = await harness.CreateTenantAsync("AKS08", "Logout Test");
        await harness.CreateUserAsync(tenant.Id, "logout@atlasweb.local", password: "Strong!123");

        var service = new AuthService(
            harness.DbContext,
            BuildAuthConfiguration(),
            NullLogger<AuthService>.Instance,
            new TestEmailSender());

        var login = await service.LoginAsync(
            new LoginDto { EPosta = "logout@atlasweb.local", Sifre = "Strong!123" },
            "127.0.0.1");

        Assert.Equal(AuthStatus.Success, login.Status);

        await service.LogoutByRefreshTokenAsync(login.RefreshToken!);

        var tokenCount = await harness.DbContext.KullaniciTokenler
            .IgnoreQueryFilters()
            .CountAsync();

        Assert.Equal(0, tokenCount);
    }
}
