using AtlasWeb.Data;
using AtlasWeb.DTOs;
using AtlasWeb.Models; // KullaniciRol sabitleri bu namespace'de
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.RateLimiting;

namespace AtlasWeb.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AtlasDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(AtlasDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("register-admin")]
        [Authorize(Roles = KullaniciRol.Admin)] // Sadece mevcut Admin'ler yeni Admin kaydedebilir
        public async Task<IActionResult> RegisterAdmin([FromBody] RegisterAdminDto dto)
        {
            if (await _context.Kullanicilar.AnyAsync(u => u.EPosta.ToLower() == dto.EPosta.ToLower()))
                return BadRequest(new { hata = "Bu e-posta adresi sistemde kayıtlıdır." });

            var yeniAdmin = new Kullanici
            {
                Id = Services.IdGenerator.CreateV7(),
                Ad = dto.Ad,
                Soyad = dto.Soyad,
                EPosta = dto.EPosta,
                SifreHash = BCrypt.Net.BCrypt.HashPassword(dto.Sifre),
                Rol = KullaniciRol.Admin,
                MusteriId = Guid.Empty,
                AktifMi = true,
                KayitTarihi = DateTime.UtcNow
            };

            _context.Kullanicilar.Add(yeniAdmin);
            await _context.SaveChangesAsync();
            return Ok(new { mesaj = "Admin yetkili kullanıcı kaydı başarıyla oluşturulmuştur." });
        }

        [HttpPost("register-user")]
        [Authorize(Roles = KullaniciRol.Admin)] // Sadece Admin'ler yeni şirket kullanıcısı ekleyebilir
        public async Task<IActionResult> RegisterUser([FromBody] RegisterUserDto dto)
        {
            if (await _context.Kullanicilar.AnyAsync(u => u.EPosta.ToLower() == dto.EPosta.ToLower()))
                return BadRequest(new { hata = "Bu e-posta adresi sistemde kayıtlıdır." });

            var sirketVarMi = await _context.Musteriler.AnyAsync(m => m.Id == dto.MusteriId);
            if (!sirketVarMi) return BadRequest(new { hata = "Belirtilen Şirket/Müşteri ID bulunamadı." });

            var yeniKullanici = new Kullanici
            {
                Id = Services.IdGenerator.CreateV7(),
                Ad = dto.Ad,
                Soyad = dto.Soyad,
                EPosta = dto.EPosta,
                SifreHash = BCrypt.Net.BCrypt.HashPassword(dto.Sifre),
                Rol = KullaniciRol.User,
                MusteriId = dto.MusteriId,
                AktifMi = true,
                KayitTarihi = DateTime.UtcNow
            };

            _context.Kullanicilar.Add(yeniKullanici);
            await _context.SaveChangesAsync();
            return Ok(new { mesaj = "Kullanıcı kaydı ilgili şirkete bağlı olarak başarıyla oluşturulmuştur." });
        }

        [HttpPost("login")]
        [EnableRateLimiting("LoginPolicy")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var kullanici = await _context.Kullanicilar.FirstOrDefaultAsync(u => u.EPosta == dto.EPosta);
            
            if (kullanici == null)
                return Unauthorized(new { hata = "Belirtilen e-posta adresine ait kullanıcı kaydı bulunamadı." });

            if (!kullanici.AktifMi)
                return Unauthorized(new { hata = "Kullanıcı hesabı aktif değildir. Lütfen sistem yöneticinizle iletişime geçiniz." });

            if (!BCrypt.Net.BCrypt.Verify(dto.Sifre, kullanici.SifreHash))
                return Unauthorized(new { hata = "Girdiğiniz şifre hatalıdır." });

            var accessToken = CreateAccessToken(kullanici);
            var plainRefreshToken = GenerateRefreshToken();

            kullanici.RefreshToken = BCrypt.Net.BCrypt.HashPassword(plainRefreshToken);
            kullanici.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            
            _context.AuditLogs.Add(new AuditLog 
            {
                EntityName = "Auth",
                EntityId = kullanici.Id.ToString(),
                Action = "Login",
                UserId = kullanici.Id.ToString()
            });

            await _context.SaveChangesAsync();

            return Ok(new TokenDto { AccessToken = accessToken, RefreshToken = plainRefreshToken });
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] TokenDto tokenDto)
        {
            if (tokenDto is null) return BadRequest();

            var principal = GetPrincipalFromExpiredToken(tokenDto.AccessToken);
            if (principal == null) return BadRequest();

            var userIdString = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdString, out Guid userId)) return BadRequest();

            var kullanici = await _context.Kullanicilar.FindAsync(userId);

            // 🛡️ Detaylı hata mesajları eklendi
            if (kullanici == null)
                return BadRequest(new { hata = "Belirtilen kullanıcı kaydı bulunamadı." });

            if (!kullanici.AktifMi)
                return Unauthorized(new { hata = "Kullanıcı hesabı aktif değildir. Lütfen sistem yöneticinizle iletişime geçiniz." });

            if (string.IsNullOrEmpty(kullanici.RefreshToken) || !BCrypt.Net.BCrypt.Verify(tokenDto.RefreshToken, kullanici.RefreshToken))
                return BadRequest(new { hata = "Oturum bilgileri doğrulanamadı. Lütfen sisteme yeniden giriş yapınız." });

            if (kullanici.RefreshTokenExpiryTime <= DateTime.UtcNow)
                return BadRequest(new { hata = "Oturum süreniz sona ermiştir. Lütfen sisteme yeniden giriş yapınız." });

            var newAccessToken = CreateAccessToken(kullanici);
            var newPlainRefreshToken = GenerateRefreshToken();

            kullanici.RefreshToken = BCrypt.Net.BCrypt.HashPassword(newPlainRefreshToken);
            kullanici.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _context.SaveChangesAsync();

            return Ok(new TokenDto { AccessToken = newAccessToken, RefreshToken = newPlainRefreshToken });
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdString, out Guid userId)) return BadRequest();

            var kullanici = await _context.Kullanicilar.FindAsync(userId);
            if (kullanici != null)
            {
                kullanici.RefreshToken = null;
                kullanici.RefreshTokenExpiryTime = null;

                _context.AuditLogs.Add(new AuditLog 
                {
                    EntityName = "Auth",
                    EntityId = userId.ToString(),
                    Action = "Logout",
                    UserId = userId.ToString()
                });

                await _context.SaveChangesAsync();
            }
            return Ok(new { mesaj = "Güvenli çıkış işlemi başarıyla gerçekleştirildi." });
        }

        private string CreateAccessToken(Kullanici kullanici)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!);
            var mid = (kullanici.MusteriId != Guid.Empty) ? kullanici.MusteriId.ToString() : "";

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, kullanici.Id.ToString()),
                    new Claim(ClaimTypes.Name, kullanici.Ad),
                    new Claim(ClaimTypes.Role, kullanici.Rol),
                    new Claim("MusteriId", mid)
                }),
                Expires = DateTime.UtcNow.AddMinutes(15),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private ClaimsPrincipal? GetPrincipalFromExpiredToken(string? token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = true,
                ValidateIssuer = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidAudience = _configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!)),
                ValidateLifetime = false
            };
            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
                if (securityToken is not JwtSecurityToken jwt || !jwt.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                    return null;
                return principal;
            }
            catch { return null; }
        }
    }
}