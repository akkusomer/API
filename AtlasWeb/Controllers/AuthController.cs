using System.Security.Claims;
using AtlasWeb.Data;
using AtlasWeb.DTOs;
using AtlasWeb.Models;
using AtlasWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace AtlasWeb.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly AtlasDbContext _context;

        public AuthController(IAuthService authService, AtlasDbContext context)
        {
            _authService = authService;
            _context     = context;
        }

        // ── LOGIN ─────────────────────────────────────────────────────────────────
        /// <summary>
        /// Kullanıcı girişi. 5 başarısız denemeden sonra hesap 15 dakika kilitlenir.
        /// Rate limit: 5 istek / dakika / IP.
        /// </summary>
        [AllowAnonymous]
        [EnableRateLimiting("LoginPolicy")]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var ip         = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var deviceInfo = Request.Headers.UserAgent.ToString();

            var result = await _authService.LoginAsync(dto, ip, deviceInfo);

            switch (result.Status)
            {
                case AuthStatus.Success:
                    // 1. Header olarak dönme (Next.js proxy için)
                    Response.Headers.Append("Authorization", $"Bearer {result.AccessToken}");

                    // 2. HttpOnly Cookie olarak dönme (Güvenli oturum yönetimi için)
                    var cookieOptions = new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true, // HTTPS ortamında çalışmalı
                        SameSite = SameSiteMode.Strict,
                        Expires = DateTime.UtcNow.AddDays(7)
                    };
                    Response.Cookies.Append("refreshToken", result.RefreshToken!, cookieOptions);

                    // 3. Body içinde dönme (Mevcut yapı ve fallback için)
                    return Ok(new
                    {
                        mesaj        = "Giriş başarılı.",
                        accessToken  = result.AccessToken,
                        refreshToken = result.RefreshToken
                    });

                case AuthStatus.Locked:
                    return StatusCode(423, new
                    {
                        hata       = "Hesabınız çok fazla başarısız giriş denemesi nedeniyle geçici olarak kilitlendi.",
                        kilitBitis = result.LockoutEnd?.ToString("o")   // ISO-8601 for frontend countdown
                    });

                case AuthStatus.Invalid:
                    return Unauthorized(new
                    {
                        hata = "E-posta veya şifre hatalı."
                    });

                default:
                    return StatusCode(500, new { hata = "Beklenmeyen bir hata oluştu." });
            }
        }

        // ── REFRESH TOKEN ─────────────────────────────────────────────────────────
        /// <summary>
        /// Yeni bir access + refresh token çifti üretir. Eski refresh token geçersiz kılınır (rotation).
        /// </summary>
        [AllowAnonymous]
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] TokenDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.RefreshToken))
                return BadRequest(new { hata = "Refresh token gerekli." });

            var ip     = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var result = await _authService.RefreshAsync(dto.RefreshToken, ip);

            switch (result.Status)
            {
                case AuthStatus.Success:
                    Response.Headers.Append("Authorization", $"Bearer {result.AccessToken}");

                    var cookieOptions = new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Strict,
                        Expires = DateTime.UtcNow.AddDays(7)
                    };
                    Response.Cookies.Append("refreshToken", result.RefreshToken!, cookieOptions);

                    return Ok(new
                    {
                        mesaj        = "Token yenilendi.",
                        accessToken  = result.AccessToken,
                        refreshToken = result.RefreshToken
                    });

                case AuthStatus.Locked:
                    return StatusCode(423, new
                    {
                        hata       = "Hesabınız kilitlenmiş.",
                        kilitBitis = result.LockoutEnd?.ToString("o")
                    });

                default:
                    return Unauthorized(new { hata = "Geçersiz veya süresi dolmuş oturum. Lütfen yeniden giriş yapın." });
            }
        }

        // ── LOGOUT ────────────────────────────────────────────────────────────────
        /// <summary>
        /// Tüm cihazlardaki refresh tokenları iptal eder.
        /// </summary>
        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (idClaim is null || !Guid.TryParse(idClaim, out var userId))
                return Unauthorized();

            await _authService.LogoutAsync(userId);

            // Cookie'leri temizle
            Response.Cookies.Delete("refreshToken");

            return Ok(new { mesaj = "Tüm oturumlar başarıyla kapatıldı." });
        }

        // ── REGISTER ─────────────────────────────────────────────────────────────
        // TODO: Move to a dedicated UserService when user management grows.
        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterUserDto dto)
        {
            if (await _context.Kullanicilar
                    .IgnoreQueryFilters()
                    .AnyAsync(u => u.EPosta == dto.EPosta))
                return BadRequest(new { hata = "Bu e-posta zaten kayıtlı." });

            var musteriVarMi = await _context.Musteriler.AnyAsync(m => m.Id == dto.MusteriId);
            if (!musteriVarMi)
                return BadRequest(new { hata = "Geçersiz müşteri (şirket) bilgisi." });

            var yeni = new Kullanici
            {
                Ad        = dto.Ad,
                Soyad     = dto.Soyad,
                EPosta    = dto.EPosta,
                SifreHash = BCrypt.Net.BCrypt.HashPassword(dto.Sifre),
                Rol       = KullaniciRol.User,
                MusteriId = dto.MusteriId,
                AktifMi   = true
            };

            _context.Kullanicilar.Add(yeni);
            await _context.SaveChangesAsync();

            return Ok(new { mesaj = "Kayıt tamamlandı." });
        }
    }
}
