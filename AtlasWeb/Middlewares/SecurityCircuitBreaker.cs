using Serilog;
using Microsoft.Extensions.Caching.Distributed;

namespace AtlasWeb.Middlewares
{
    public class SecurityCircuitBreaker
    {
        private readonly RequestDelegate _next;

        public SecurityCircuitBreaker(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext context, IDistributedCache cache)
        {
            var user = context.User?.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "Anonymous";
            var cacheKey = $"FailedAttempts_{user}";

            var countStr = await cache.GetStringAsync(cacheKey);
            int count = string.IsNullOrEmpty(countStr) ? 0 : int.Parse(countStr);

            if (count > 10)
            {
                Log.Fatal("🚨 CIRCUIT BREAKER: User {User} blocked due to excessive security violations!", user);
                context.Response.StatusCode = 429; // Too Many Requests / Blocked
                return;
            }

            try { await _next(context); }
            catch (UnauthorizedAccessException)
            {
                count++;
                await cache.SetStringAsync(cacheKey, count.ToString(), new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
                });
                throw; // ExceptionMiddleware yakalayacak
            }
        }
    }
}