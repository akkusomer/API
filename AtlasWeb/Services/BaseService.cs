using AtlasWeb.Models;
using Serilog;

namespace AtlasWeb.Services
{
    public abstract class BaseService
    {
        protected readonly ICurrentUserService _currentUserService;

        protected BaseService(ICurrentUserService currentUserService) => _currentUserService = currentUserService;

        protected void ValidateTenantAccess(ITenantEntity entity)
        {
            if (!_currentUserService.IsSystemAdmin && entity.MusteriId != _currentUserService.MusteriId)
            {
                Log.Fatal(
                    "!!! SECURITY BREACH DETECTED !!! User {User} accessed {Target}",
                    _currentUserService.EPosta,
                    entity.MusteriId);
                throw new UnauthorizedAccessException("Guvenlik ihlali: Erisim reddedildi.");
            }
        }
    }
}
