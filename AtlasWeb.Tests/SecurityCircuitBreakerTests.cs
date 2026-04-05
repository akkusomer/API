using AtlasWeb.Middlewares;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace AtlasWeb.Tests;

public class SecurityCircuitBreakerTests
{
    [Fact]
    public async Task DoesNotCountUnauthorizedResponses()
    {
        var cache = CreateCache();
        var middleware = new SecurityCircuitBreaker(_ =>
        {
            _.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        });

        for (var index = 0; index < 12; index++)
        {
            var context = CreateContext("/api/Fatura");
            await middleware.InvokeAsync(context, cache);
            Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        }
    }

    [Fact]
    public async Task BypassesAuthEndpoints()
    {
        var cache = CreateCache();
        await cache.SetStringAsync("SecurityViolations_127.0.0.1", "10");

        var middleware = new SecurityCircuitBreaker(_ =>
        {
            _.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        });

        var context = CreateContext("/api/auth/refresh-token");
        await middleware.InvokeAsync(context, cache);

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    [Fact]
    public async Task BlocksAfterRepeatedForbiddenResponses()
    {
        var cache = CreateCache();
        var middleware = new SecurityCircuitBreaker(_ =>
        {
            _.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        });

        for (var index = 0; index < 10; index++)
        {
            var context = CreateContext("/api/Fatura");
            await middleware.InvokeAsync(context, cache);
        }

        var blockedContext = CreateContext("/api/Fatura");
        await middleware.InvokeAsync(blockedContext, cache);

        Assert.Equal(StatusCodes.Status429TooManyRequests, blockedContext.Response.StatusCode);
    }

    private static IDistributedCache CreateCache()
        => new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));

    private static DefaultHttpContext CreateContext(string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");
        return context;
    }
}
