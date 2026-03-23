using System.Security.Claims;

namespace AtlasWeb.Services
{
    public interface ICurrentUserService
    {
        Guid? MusteriId { get; }
        string? EPosta { get; }
        bool IsAdmin { get; }
    }

    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _accessor;
        public CurrentUserService(IHttpContextAccessor accessor) => _accessor = accessor;

        public Guid? MusteriId => Guid.TryParse(_accessor.HttpContext?.User?.FindFirst("MusteriId")?.Value, out var id) ? id : null;

        // 🛡️ Fallback: HttpContext yoksa "System" döner
        public string? EPosta => _accessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value ?? "System_Automated_Job";

        public bool IsAdmin => _accessor.HttpContext?.User?.IsInRole("Admin") ?? false;
    }
}