using AtlasWeb.DTOs;

namespace AtlasWeb.Services
{
    public interface IAuthService
    {
        Task<AuthResult> LoginAsync(LoginDto dto, string ipAddress, string? deviceInfo = null);
        Task<AuthResult> RefreshAsync(string refreshToken, string ipAddress);
        Task LogoutAsync(Guid userId);
    }

    // ── Result Types ──────────────────────────────────────────────────────────────

    public enum AuthStatus { Success, Invalid, Locked, Error }

    public sealed class AuthResult
    {
        public AuthStatus Status { get; private init; }
        public string? AccessToken { get; private init; }
        public string? RefreshToken { get; private init; }
        public DateTime? LockoutEnd { get; private init; }

        public static AuthResult Success(string access, string refresh) =>
            new() { Status = AuthStatus.Success, AccessToken = access, RefreshToken = refresh };

        public static AuthResult Invalid() =>
            new() { Status = AuthStatus.Invalid };

        public static AuthResult Locked(DateTime lockedUntil) =>
            new() { Status = AuthStatus.Locked, LockoutEnd = lockedUntil };
    }
}
