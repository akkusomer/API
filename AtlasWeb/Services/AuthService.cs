using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AtlasWeb.Data;
using AtlasWeb.DTOs;
using AtlasWeb.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace AtlasWeb.Services
{
    public class AuthService : IAuthService
    {
        private static readonly string DummyPasswordHash = BCrypt.Net.BCrypt.HashPassword("AtlasWebDummyPassword#2026");

        private readonly AtlasDbContext _context;
        private readonly IConfiguration _config;
        private readonly ILogger<AuthService> _logger;
        private readonly IEmailSender _emailSender;

        private int MaxFailedAttempts => _config.GetValue("Auth:MaxFailedAttempts", 5);
        private int LockoutMinutes => _config.GetValue("Auth:LockoutMinutes", 15);
        private int AccessTokenMinutes => _config.GetValue("Auth:AccessTokenExpiryMinutes", 15);
        private int RefreshTokenDays => _config.GetValue("Auth:RefreshTokenExpiryDays", 7);
        private int PasswordResetTokenMinutes => _config.GetValue("Auth:PasswordResetTokenExpiryMinutes", 30);

        public AuthService(
            AtlasDbContext context,
            IConfiguration config,
            ILogger<AuthService> logger,
            IEmailSender emailSender)
        {
            _context = context;
            _config = config;
            _logger = logger;
            _emailSender = emailSender;
        }

        public async Task<AuthResult> LoginAsync(LoginDto dto, string ipAddress, string? deviceInfo = null)
        {
            var emailNorm = IdentityNormalizer.NormalizeEmail(dto.EPosta);

            var matchingUsers = await _context.Kullanicilar
                .IgnoreQueryFilters()
                .Where(u => u.EPosta == emailNorm && u.AktifMi)
                .Take(2)
                .ToListAsync();

            if (matchingUsers.Count > 1)
            {
                _logger.LogError(
                    "[LOGIN BLOCKED] Duplicate active accounts found for normalized email. Email: {Email} | IP: {IP}",
                    emailNorm,
                    ipAddress);
                return AuthResult.Invalid();
            }

            var kullanici = matchingUsers.SingleOrDefault();
            var passwordHash = kullanici?.SifreHash ?? DummyPasswordHash;
            var passwordIsValid = BCrypt.Net.BCrypt.Verify(dto.Sifre, passwordHash) && kullanici is not null;

            if (kullanici is null)
            {
                _logger.LogWarning(
                    "[LOGIN FAILED] Unknown email attempted. Email: {Email} | IP: {IP}",
                    emailNorm,
                    ipAddress);
                return AuthResult.Invalid();
            }

            if (kullanici.LockoutEnd.HasValue && kullanici.LockoutEnd > DateTime.UtcNow)
            {
                _logger.LogWarning(
                    "[ACCOUNT LOCKED] Login blocked for locked account. Email: {Email} | IP: {IP} | Until: {Until}",
                    emailNorm,
                    ipAddress,
                    kullanici.LockoutEnd);
                return AuthResult.Locked(kullanici.LockoutEnd.Value);
            }

            if (!passwordIsValid)
            {
                kullanici.FailedLoginCount++;

                if (kullanici.FailedLoginCount >= MaxFailedAttempts)
                {
                    kullanici.LockoutEnd = DateTime.UtcNow.AddMinutes(LockoutMinutes);
                    await _context.SaveChangesAsync();

                    _logger.LogWarning(
                        "[ACCOUNT LOCKOUT] Account locked after {Count} failed attempts. Email: {Email} | IP: {IP} | Until: {Until}",
                        kullanici.FailedLoginCount,
                        emailNorm,
                        ipAddress,
                        kullanici.LockoutEnd);

                    return AuthResult.Locked(kullanici.LockoutEnd.Value);
                }

                await _context.SaveChangesAsync();

                _logger.LogWarning(
                    "[LOGIN FAILED] Wrong password. Email: {Email} | IP: {IP} | Attempt: {Count}/{Max}",
                    emailNorm,
                    ipAddress,
                    kullanici.FailedLoginCount,
                    MaxFailedAttempts);

                return AuthResult.Invalid();
            }

            kullanici.FailedLoginCount = 0;
            kullanici.LockoutEnd = null;

            var companyName = await ResolveCompanyNameAsync(kullanici.MusteriId);
            var (accessToken, jti) = CreateAccessToken(kullanici, companyName);
            var (rawRefresh, hashedRefresh) = GenerateRefreshToken();

            _context.KullaniciTokenler.Add(new KullaniciToken
            {
                KullaniciId = kullanici.Id,
                RefreshTokenHash = hashedRefresh,
                ExpiryTime = DateTime.UtcNow.AddDays(RefreshTokenDays),
                DeviceInfo = string.IsNullOrWhiteSpace(deviceInfo) ? "Unknown" : deviceInfo,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "[LOGIN SUCCESS] Email: {Email} | IP: {IP} | UserId: {UserId} | JTI: {Jti}",
                emailNorm,
                ipAddress,
                kullanici.Id,
                jti);

            return AuthResult.Success(accessToken, rawRefresh);
        }

        public async Task<AuthResult> RefreshAsync(string rawRefreshToken, string ipAddress)
        {
            var hashed = HashToken(rawRefreshToken);

            var tokenRecord = await _context.KullaniciTokenler
                .Include(t => t.Kullanici)
                .FirstOrDefaultAsync(t =>
                    t.RefreshTokenHash == hashed &&
                    t.ExpiryTime > DateTime.UtcNow &&
                    t.Kullanici != null &&
                    t.Kullanici.AktifMi);

            if (tokenRecord is null)
            {
                _logger.LogWarning("[REFRESH FAILED] Invalid or expired refresh token used. IP: {IP}", ipAddress);
                return AuthResult.Invalid();
            }

            var kullanici = tokenRecord.Kullanici!;

            if (kullanici.LockoutEnd.HasValue && kullanici.LockoutEnd > DateTime.UtcNow)
            {
                return AuthResult.Locked(kullanici.LockoutEnd.Value);
            }

            _context.KullaniciTokenler.Remove(tokenRecord);

            var companyName = await ResolveCompanyNameAsync(kullanici.MusteriId);
            var (newAccess, jti) = CreateAccessToken(kullanici, companyName);
            var (newRawRefresh, newHashedRefresh) = GenerateRefreshToken();

            _context.KullaniciTokenler.Add(new KullaniciToken
            {
                KullaniciId = kullanici.Id,
                RefreshTokenHash = newHashedRefresh,
                ExpiryTime = DateTime.UtcNow.AddDays(RefreshTokenDays),
                DeviceInfo = tokenRecord.DeviceInfo,
                CreatedAt = DateTime.UtcNow
            });

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                _logger.LogWarning("[REFRESH CONFLICT] Token already consumed concurrently. IP: {IP}", ipAddress);
                return AuthResult.Invalid();
            }
            catch (DbUpdateException)
            {
                _logger.LogWarning("[REFRESH CONFLICT] Token rotation hit a unique constraint. IP: {IP}", ipAddress);
                return AuthResult.Invalid();
            }

            _logger.LogInformation(
                "[TOKEN REFRESH] UserId: {UserId} | IP: {IP} | JTI: {Jti}",
                kullanici.Id,
                ipAddress,
                jti);

            return AuthResult.Success(newAccess, newRawRefresh);
        }

        public async Task LogoutAsync(Guid userId)
        {
            var tokens = await _context.KullaniciTokenler
                .Where(t => t.KullaniciId == userId)
                .ToListAsync();

            _context.KullaniciTokenler.RemoveRange(tokens);
            await _context.SaveChangesAsync();

            _logger.LogInformation("[LOGOUT] UserId: {UserId} | Sessions revoked: {Count}", userId, tokens.Count);
        }

        public async Task LogoutByRefreshTokenAsync(string refreshToken)
        {
            var hashed = HashToken(refreshToken);
            var tokens = await _context.KullaniciTokenler
                .Where(t => t.RefreshTokenHash == hashed)
                .ToListAsync();

            if (tokens.Count == 0)
            {
                _logger.LogInformation("[LOGOUT] Refresh token logout requested but no active session matched.");
                return;
            }

            _context.KullaniciTokenler.RemoveRange(tokens);
            await _context.SaveChangesAsync();

            _logger.LogInformation("[LOGOUT] Refresh token session revoked. Sessions revoked: {Count}", tokens.Count);
        }

        public async Task<PasswordResetRequestStatus> RequestPasswordResetAsync(
            ForgotPasswordRequestDto dto,
            string resetPageUrl,
            string ipAddress)
        {
            if (!_emailSender.IsConfigured)
            {
                _logger.LogWarning("[PASSWORD RESET] Email service is not configured. IP: {IP}", ipAddress);
                return PasswordResetRequestStatus.ServiceUnavailable;
            }

            var emailNorm = IdentityNormalizer.NormalizeEmail(dto.EPosta);
            var kullanici = await _context.Kullanicilar
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.EPosta == emailNorm && u.AktifMi);

            if (kullanici is null)
            {
                _logger.LogInformation(
                    "[PASSWORD RESET] Reset requested for unknown email. Email: {Email} | IP: {IP}",
                    emailNorm,
                    ipAddress);
                return PasswordResetRequestStatus.Accepted;
            }

            var now = DateTime.UtcNow;

            var staleTokens = await _context.KullaniciSifreSifirlamaTokenler
                .Where(t =>
                    t.KullaniciId == kullanici.Id &&
                    (t.ConsumedAt != null || t.ExpiryTime <= now))
                .ToListAsync();

            if (staleTokens.Count > 0)
            {
                _context.KullaniciSifreSifirlamaTokenler.RemoveRange(staleTokens);
            }

            var rawToken = GenerateOneTimeToken();
            var expiresAtUtc = now.AddMinutes(PasswordResetTokenMinutes);

            _context.KullaniciSifreSifirlamaTokenler.Add(new KullaniciSifreSifirlamaToken
            {
                KullaniciId = kullanici.Id,
                TokenHash = HashToken(rawToken),
                ExpiryTime = expiresAtUtc,
                CreatedAt = now,
                RequestedIpAddress = ipAddress
            });

            try
            {
                await _context.SaveChangesAsync();

                var fullName = $"{kullanici.Ad} {kullanici.Soyad}".Trim();
                var resetUrl = BuildResetUrl(resetPageUrl, rawToken);
                await _emailSender.SendPasswordResetAsync(
                    kullanici.EPosta,
                    string.IsNullOrWhiteSpace(fullName) ? kullanici.EPosta : fullName,
                    resetUrl,
                    expiresAtUtc);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[PASSWORD RESET] Failed to send password reset email. Email: {Email} | IP: {IP}",
                    emailNorm,
                    ipAddress);

                var failedTokens = await _context.KullaniciSifreSifirlamaTokenler
                    .Where(t => t.KullaniciId == kullanici.Id)
                    .ToListAsync();
                _context.KullaniciSifreSifirlamaTokenler.RemoveRange(failedTokens);
                await _context.SaveChangesAsync();

                return PasswordResetRequestStatus.ServiceUnavailable;
            }

            _logger.LogInformation(
                "[PASSWORD RESET] Reset email queued. Email: {Email} | IP: {IP}",
                emailNorm,
                ipAddress);

            return PasswordResetRequestStatus.Accepted;
        }

        public async Task<PasswordResetStatus> ResetPasswordAsync(ResetPasswordDto dto, string ipAddress)
        {
            var hashedToken = HashToken(dto.Token);

            var tokenRecord = await _context.KullaniciSifreSifirlamaTokenler
                .Include(t => t.Kullanici)
                .FirstOrDefaultAsync(t => t.TokenHash == hashedToken && t.ConsumedAt == null);

            if (tokenRecord is null || tokenRecord.Kullanici is null || !tokenRecord.Kullanici.AktifMi)
            {
                _logger.LogWarning("[PASSWORD RESET] Invalid reset token used. IP: {IP}", ipAddress);
                return PasswordResetStatus.Invalid;
            }

            if (tokenRecord.ExpiryTime <= DateTime.UtcNow)
            {
                _context.KullaniciSifreSifirlamaTokenler.Remove(tokenRecord);
                await _context.SaveChangesAsync();

                _logger.LogWarning(
                    "[PASSWORD RESET] Expired reset token used. UserId: {UserId} | IP: {IP}",
                    tokenRecord.KullaniciId,
                    ipAddress);

                return PasswordResetStatus.Expired;
            }

            var kullanici = tokenRecord.Kullanici;
            kullanici.SifreHash = BCrypt.Net.BCrypt.HashPassword(dto.YeniSifre);
            kullanici.FailedLoginCount = 0;
            kullanici.LockoutEnd = null;

            var refreshTokens = await _context.KullaniciTokenler
                .Where(t => t.KullaniciId == kullanici.Id)
                .ToListAsync();
            var resetTokens = await _context.KullaniciSifreSifirlamaTokenler
                .Where(t => t.KullaniciId == kullanici.Id)
                .ToListAsync();

            _context.KullaniciTokenler.RemoveRange(refreshTokens);
            _context.KullaniciSifreSifirlamaTokenler.RemoveRange(resetTokens);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[PASSWORD RESET] Failed to update password. UserId: {UserId} | IP: {IP}",
                    kullanici.Id,
                    ipAddress);
                return PasswordResetStatus.Error;
            }

            _logger.LogInformation(
                "[PASSWORD RESET] Password changed successfully. UserId: {UserId} | IP: {IP}",
                kullanici.Id,
                ipAddress);

            return PasswordResetStatus.Success;
        }

        private async Task<string?> ResolveCompanyNameAsync(Guid musteriId)
        {
            return await _context.Musteriler
                .IgnoreQueryFilters()
                .Where(m => m.Id == musteriId)
                .Select(m => m.Unvan)
                .FirstOrDefaultAsync();
        }

        private (string Token, string Jti) CreateAccessToken(Kullanici kullanici, string? companyName)
        {
            var jwtKey = _config["Jwt:Key"]
                ?? Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
                ?? throw new InvalidOperationException("Jwt:Key tanimli degil.");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var jti = Guid.NewGuid().ToString();

            var displayName = $"{kullanici.Ad} {kullanici.Soyad}".Trim();
            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = kullanici.EPosta;
            }

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Jti, jti),
                new(ClaimTypes.NameIdentifier, kullanici.Id.ToString()),
                new(ClaimTypes.Name, displayName),
                new(ClaimTypes.Email, kullanici.EPosta),
                new(ClaimTypes.Role, kullanici.Rol),
                new("MusteriId", kullanici.MusteriId.ToString())
            };

            if (!string.IsNullOrWhiteSpace(companyName))
            {
                claims.Add(new Claim("CompanyName", companyName.Trim()));
                claims.Add(new Claim("MusteriUnvan", companyName.Trim()));
            }

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(AccessTokenMinutes),
                signingCredentials: creds);

            return (new JwtSecurityTokenHandler().WriteToken(token), jti);
        }

        private static (string Raw, string Hashed) GenerateRefreshToken()
        {
            var bytes = new byte[64];
            RandomNumberGenerator.Fill(bytes);
            var raw = Convert.ToBase64String(bytes);
            return (raw, HashToken(raw));
        }

        private static string GenerateOneTimeToken()
        {
            var bytes = new byte[32];
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToHexString(bytes);
        }

        private static string BuildResetUrl(string resetPageUrl, string rawToken)
        {
            var separator = resetPageUrl.Contains('?', StringComparison.Ordinal) ? "&" : "?";
            return $"{resetPageUrl}{separator}token={Uri.EscapeDataString(rawToken)}";
        }

        private static string HashToken(string raw)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
            return Convert.ToHexString(bytes);
        }
    }
}
