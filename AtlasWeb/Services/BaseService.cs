using AtlasWeb.Models;
using Serilog;

namespace AtlasWeb.Services
{
    public abstract class BaseService
    {
        protected readonly ICurrentUserService _currentUserService;
        protected BaseService(ICurrentUserService currentUserService) => _currentUserService = currentUserService;

        // 🛡️ V6: Filter bypass edilse bile servis katmanı bloklar
        protected void ValidateTenantAccess(ITenantEntity entity)
        {
            if (!_currentUserService.IsAdmin && entity.MusteriId != _currentUserService.MusteriId)
            {
                Log.Fatal("!!! SECURITY BREACH DETECTED !!! User {User} accessed {Target}",
                    _currentUserService.EPosta, entity.MusteriId);
                throw new UnauthorizedAccessException("Güvenlik ihlali: Erişim reddedildi.");
            }
        }
    }
}