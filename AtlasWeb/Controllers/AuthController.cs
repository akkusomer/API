using System.Security.Claims;
using AtlasWeb.Data;
using AtlasWeb.DTOs;
using AtlasWeb.Models;
using AtlasWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace AtlasWeb.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private const string RefreshTokenCookieName = "refreshToken";
        private const string RefreshTokenCookiePath = "/api/auth";

        private readonly IAuthService _authService;
        private readonly AtlasDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IAuthService authService,
            AtlasDbContext context,
            ICurrentUserService currentUserService,
            IConfiguration configuration,
            ILogger<AuthController> logger)
        {
            _authService = authService;
            _context = context;
            _currentUserService = currentUserService;
            _configuration = configuration;
            _logger = logger;
        }

        [AllowAnonymous]
        [EnableRateLimiting("LoginPolicy")]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var deviceInfo = Request.Headers.UserAgent.ToString();
            var result = await _authService.LoginAsync(dto, ip, deviceInfo);

            switch (result.Status)
            {
                case AuthStatus.Success:
                    SetNoStoreHeaders();
                    Response.Headers.Append("Authorization", $"Bearer {result.AccessToken}");
                    AppendRefreshTokenCookie(result.RefreshToken!);
                    return Ok(new
                    {
                        mesaj = "Giris basarili.",
                        accessToken = result.AccessToken
                    });

                case AuthStatus.Locked:
                    return StatusCode(423, new
                    {
                        hata = "Hesabiniz gecici olarak kilitlendi.",
                        kilitBitis = result.LockoutEnd?.ToString("o")
                    });

                case AuthStatus.Invalid:
                    return Unauthorized(new { hata = "E-posta veya sifre hatali." });

                default:
                    return StatusCode(500, new { hata = "Beklenmeyen bir hata olustu." });
            }
        }

        [AllowAnonymous]
        [EnableRateLimiting("ForgotPasswordPolicy")]
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto dto)
        {
            string resetPasswordUrl;
            try
            {
                resetPasswordUrl = BuildResetPasswordUrl();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "[PASSWORD RESET] Public reset password URL is not configured safely.");
                return StatusCode(503, new
                {
                    hata = "Sifre sifirlama servisi su anda kullanilamiyor. Lutfen daha sonra tekrar deneyin."
                });
            }

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var result = await _authService.RequestPasswordResetAsync(dto, resetPasswordUrl, ip);

            return result switch
            {
                PasswordResetRequestStatus.Accepted => Ok(new
                {
                    mesaj = "Eger hesabiniz sistemde kayitliysa sifre sifirlama baglantisi e-posta adresinize gonderildi."
                }),
                PasswordResetRequestStatus.ServiceUnavailable => StatusCode(503, new
                {
                    hata = "E-posta servisi su anda kullanilamiyor. Lutfen daha sonra tekrar deneyin."
                }),
                _ => StatusCode(500, new { hata = "Sifre sifirlama istegi olusturulamadi." })
            };
        }

        [AllowAnonymous]
        [EnableRateLimiting("PasswordResetPolicy")]
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var result = await _authService.ResetPasswordAsync(dto, ip);

            switch (result)
            {
                case PasswordResetStatus.Success:
                    SetNoStoreHeaders();
                    DeleteRefreshTokenCookie();
                    return Ok(new { mesaj = "Sifreniz guncellendi. Yeni sifrenizle giris yapabilirsiniz." });

                case PasswordResetStatus.Expired:
                    return BadRequest(new { hata = "Sifre sifirlama baglantisinin suresi dolmus." });

                case PasswordResetStatus.Invalid:
                    return BadRequest(new { hata = "Gecersiz veya kullanilmis sifre sifirlama baglantisi." });

                default:
                    return StatusCode(500, new { hata = "Sifre guncellenemedi." });
            }
        }

        [AllowAnonymous]
        [EnableRateLimiting("RefreshPolicy")]
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken()
        {
            var rawRefreshToken = Request.Cookies[RefreshTokenCookieName];

            if (string.IsNullOrWhiteSpace(rawRefreshToken))
            {
                return Unauthorized(new { hata = "Gecerli bir oturum bulunamadi." });
            }

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var result = await _authService.RefreshAsync(rawRefreshToken, ip);

            switch (result.Status)
            {
                case AuthStatus.Success:
                    SetNoStoreHeaders();
                    Response.Headers.Append("Authorization", $"Bearer {result.AccessToken}");
                    AppendRefreshTokenCookie(result.RefreshToken!);
                    return Ok(new
                    {
                        mesaj = "Token yenilendi.",
                        accessToken = result.AccessToken
                    });

                case AuthStatus.Locked:
                    return StatusCode(423, new
                    {
                        hata = "Hesabiniz kilitli.",
                        kilitBitis = result.LockoutEnd?.ToString("o")
                    });

                default:
                    DeleteRefreshTokenCookie();
                    return Unauthorized(new { hata = "Gecersiz veya suresi dolmus oturum." });
            }
        }

        [AllowAnonymous]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var rawRefreshToken = Request.Cookies[RefreshTokenCookieName];

            if (idClaim is not null && Guid.TryParse(idClaim, out var userId))
            {
                await _authService.LogoutAsync(userId);
            }
            else if (!string.IsNullOrWhiteSpace(rawRefreshToken))
            {
                await _authService.LogoutByRefreshTokenAsync(rawRefreshToken);
            }

            SetNoStoreHeaders();
            DeleteRefreshTokenCookie();
            return Ok(new { mesaj = "Oturum kapatildi." });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterUserDto dto)
        {
            var normalizedEmail = IdentityNormalizer.NormalizeEmail(dto.EPosta);
            var emailExists = await _context.Kullanicilar
                .IgnoreQueryFilters()
                .AnyAsync(u => u.EPosta == normalizedEmail);

            if (emailExists)
            {
                return BadRequest(new { hata = "Bu e-posta zaten kayitli." });
            }

            var targetMusteriId = dto.MusteriId;

            if (!_currentUserService.IsSystemAdmin)
            {
                if (!_currentUserService.IsAdmin || _currentUserService.MusteriId is null)
                {
                    return Forbid();
                }

                targetMusteriId = _currentUserService.MusteriId.Value;
            }

            var musteriVarMi = await _context.Musteriler
                .IgnoreQueryFilters()
                .AnyAsync(m => m.Id == targetMusteriId && m.AktifMi);

            if (!musteriVarMi)
            {
                return BadRequest(new { hata = "Gecersiz musteri bilgisi." });
            }

            var yeni = new Kullanici
            {
                Ad = dto.Ad.Trim(),
                Soyad = dto.Soyad.Trim(),
                EPosta = normalizedEmail,
                Telefon = NormalizePhone(dto.Telefon),
                SifreHash = BCrypt.Net.BCrypt.HashPassword(dto.Sifre),
                Rol = KullaniciRol.User,
                MusteriId = targetMusteriId,
                AktifMi = true
            };

            _context.Kullanicilar.Add(yeni);
            await _context.SaveChangesAsync();

            return Ok(new { mesaj = "Kullanici kaydi tamamlandi." });
        }

        private void SetNoStoreHeaders()
        {
            Response.Headers.CacheControl = "no-store";
            Response.Headers.Pragma = "no-cache";
        }

        private void AppendRefreshTokenCookie(string refreshToken)
        {
            Response.Cookies.Append(
                RefreshTokenCookieName,
                refreshToken,
                BuildRefreshTokenCookieOptions(DateTime.UtcNow.AddDays(7)));
        }

        private void DeleteRefreshTokenCookie()
        {
            Response.Cookies.Delete(RefreshTokenCookieName, BuildRefreshTokenCookieOptions(DateTime.UtcNow.AddDays(-1)));
        }

        private CookieOptions BuildRefreshTokenCookieOptions(DateTime expires)
        {
            return new CookieOptions
            {
                HttpOnly = true,
                Secure = ShouldUseSecureCookies(),
                SameSite = SameSiteMode.Strict,
                Path = RefreshTokenCookiePath,
                Expires = expires
            };
        }

        private bool ShouldUseSecureCookies()
        {
            if (_configuration.GetValue("Auth:AllowInsecureRefreshCookie", false))
            {
                return false;
            }

            return Request.IsHttps;
        }

        private string BuildResetPasswordUrl()
        {
            var configuredUrl = _configuration["Email:ResetPasswordUrl"];
            if (!string.IsNullOrWhiteSpace(configuredUrl))
            {
                return NormalizeAbsoluteUrl(configuredUrl, allowPath: true);
            }

            var publicBaseUrl = _configuration["App:PublicBaseUrl"];
            if (!string.IsNullOrWhiteSpace(publicBaseUrl))
            {
                var normalizedBaseUrl = NormalizeAbsoluteUrl(publicBaseUrl, allowPath: false);
                return $"{normalizedBaseUrl}/templates/reset-password.html";
            }

            if (IsLoopbackHost(Request.Host.Host))
            {
                return $"{Request.Scheme}://{Request.Host.Value}/templates/reset-password.html";
            }

            throw new InvalidOperationException("Email:ResetPasswordUrl veya App:PublicBaseUrl ayarlanmadan reset linki uretilemez.");
        }

        private static string? NormalizePhone(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static bool IsLoopbackHost(string? host)
        {
            return host != null && (
                host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                || host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
                || host.Equals("::1", StringComparison.OrdinalIgnoreCase)
                || host.Equals("[::1]", StringComparison.OrdinalIgnoreCase));
        }

        private static string NormalizeAbsoluteUrl(string value, bool allowPath)
        {
            if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                throw new InvalidOperationException("Mutlak bir HTTP/HTTPS adresi bekleniyor.");
            }

            if (!allowPath && !string.IsNullOrWhiteSpace(uri.PathAndQuery) && uri.PathAndQuery != "/")
            {
                throw new InvalidOperationException("Public base URL yalnizca origin seviyesinde tanimlanmalidir.");
            }

            var normalized = allowPath
                ? uri.ToString().TrimEnd('/')
                : uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');

            return normalized;
        }
    }
}
