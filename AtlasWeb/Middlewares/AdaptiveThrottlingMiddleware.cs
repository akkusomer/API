using System.Collections.Concurrent;

namespace AtlasWeb.Middlewares
{
    public class AdaptiveThrottlingMiddleware
    {
        private readonly RequestDelegate _next;
        private static readonly ConcurrentDictionary<string, int> _violationTracker = new();

        public AdaptiveThrottlingMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext context)
        {
            var userKey = context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anon";
            if (_violationTracker.TryGetValue(userKey, out int v) && v > 3) await Task.Delay(Math.Min(v * 1000, 10000));

            try { await _next(context); }
            catch (UnauthorizedAccessException) { _violationTracker.AddOrUpdate(userKey, 1, (k, val) => val + 1); throw; }
        }
    }
}