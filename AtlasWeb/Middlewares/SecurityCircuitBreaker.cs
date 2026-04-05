using System.Security.Claims;
using Microsoft.Extensions.Caching.Distributed;
using Serilog;

namespace AtlasWeb.Middlewares
{
    public class SecurityCircuitBreaker
    {
        private readonly RequestDelegate _next;

        public SecurityCircuitBreaker(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext context, IDistributedCache cache)
        {
            if (ShouldBypass(context))
            {
                await _next(context);
                return;
            }

            var user = context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? context.Connection.RemoteIpAddress?.ToString()
                ?? "Anonymous";

            var cacheKey = $"SecurityViolations_{user}";
            var count = await GetCountAsync(cache, cacheKey);

            if (count >= 10)
            {
                Log.Warning("CIRCUIT BREAKER: {User} blocked due to repeated security violations.", user);
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new { hata = "Cok fazla guvenlik ihlali algilandi." });
                return;
            }

            try
            {
                await _next(context);

                if (context.Response.StatusCode is StatusCodes.Status403Forbidden)
                {
                    await SetCountAsync(cache, cacheKey, count + 1);
                    return;
                }

                if (count > 0 && context.Response.StatusCode < 400)
                {
                    await cache.RemoveAsync(cacheKey);
                }
            }
            catch (UnauthorizedAccessException)
            {
                await SetCountAsync(cache, cacheKey, count + 1);
                throw;
            }
        }

        private static bool ShouldBypass(HttpContext context)
        {
            var path = context.Request.Path;
            return path.StartsWithSegments("/api/auth", StringComparison.OrdinalIgnoreCase);
        }

        private static async Task<int> GetCountAsync(IDistributedCache cache, string cacheKey)
        {
            var countStr = await cache.GetStringAsync(cacheKey);
            return int.TryParse(countStr, out var count) ? count : 0;
        }

        private static Task SetCountAsync(IDistributedCache cache, string cacheKey, int count)
        {
            return cache.SetStringAsync(
                cacheKey,
                count.ToString(),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
                });
        }
    }
}
