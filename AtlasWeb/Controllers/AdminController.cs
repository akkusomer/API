using System.IO;
using AtlasWeb.Data;
using AtlasWeb.Models;
using AtlasWeb.DTOs;
using AtlasWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AtlasWeb.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly AtlasDbContext _context;

        public AdminController(AtlasDbContext context)
        {
            _context = context;
        }

        [HttpGet("aktiviteler")]
        public async Task<IActionResult> GetUserActivities()
        {
            var logs = await _context.AuditLogs
                .OrderByDescending(x => x.Timestamp)
                .Take(100)
                .Select(x => new 
                {
                    x.Id,
                    x.Action,
                    x.UserId,
                    x.Timestamp
                })
                .ToListAsync();

            return Ok(logs);
        }

        [HttpGet("sistem-hatalari")]
        public async Task<IActionResult> GetSystemErrors()
        {
            var hatalar = await _context.ErrorLogs
                .OrderByDescending(x => x.Tarih)
                .Take(100)
                .ToListAsync();

            return Ok(hatalar);
        }

        [HttpGet("dosya-loglari")]
        public async Task<IActionResult> GetFileLogs()
        {
            try
            {
                var logPath = Path.Combine(AppContext.BaseDirectory, "logs");
                if (!Directory.Exists(logPath)) return NotFound("Log dizini bulunamadı.");

                var file = Directory.GetFiles(logPath)
                    .OrderByDescending(f => f)
                    .FirstOrDefault();

                if (file == null) return NotFound("Log dosyası bulunamadı.");

                var lines = await System.IO.File.ReadAllLinesAsync(file);
                var lastLines = lines.Skip(Math.Max(0, lines.Length - 100)).ToList();

                return Ok(lastLines);
            }
            catch (Exception ex)
            {
                return BadRequest("Loglar okunurken hata: " + ex.Message);
            }
        }

        [HttpGet("musteri/{id}/kullanicilar")]
        public async Task<IActionResult> GetMusteriUsers(Guid id)
        {
            var users = await _context.Kullanicilar
                .IgnoreQueryFilters()
                .Where(u => u.MusteriId == id)
                .Select(u => new 
                {
                    u.Id,
                    u.Ad,
                    u.Soyad,
                    u.EPosta,
                    u.Rol,
                    u.AktifMi
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpPost("musteri/{id}/kullanici")]
        public async Task<IActionResult> AddUserToMusteri(Guid id, [FromBody] RegisterUserDto dto)
        {
            var musteriVarMi = await _context.Musteriler.AnyAsync(m => m.Id == id);
            if (!musteriVarMi) return BadRequest("Müşteri bulunamadı.");

            var yeni = new Kullanici
            {
                Id = IdGenerator.CreateV7(),
                Ad = dto.Ad,
                Soyad = dto.Soyad,
                EPosta = dto.EPosta,
                SifreHash = BCrypt.Net.BCrypt.HashPassword(dto.Sifre),
                Rol = KullaniciRol.User,
                MusteriId = id,
                AktifMi = true
            };

            _context.Kullanicilar.Add(yeni);
            await _context.SaveChangesAsync();

            return Ok(new { mesaj = "Kullanıcı başarıyla eklendi." });
        }

        [HttpDelete("kullanici/{id}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            var affected = await _context.Kullanicilar
                .IgnoreQueryFilters()
                .Where(u => u.Id == id)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.AktifMi, false));

            if (affected == 0) return NotFound(new { mesaj = "Kullanıcı bulunamadı." });

            return Ok(new { mesaj = "Kullanıcı pasife alındı." });
        }

        [HttpGet("yoneticiler")]
        public async Task<IActionResult> GetYoneticiler()
        {
            var users = await _context.Kullanicilar
                .IgnoreQueryFilters()
                .Where(u => u.Rol == KullaniciRol.Admin)
                .Select(u => new 
                {
                    u.Id,
                    u.Ad,
                    u.Soyad,
                    u.EPosta,
                    u.Rol,
                    u.AktifMi
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpPost("yonetici")]
        public async Task<IActionResult> AddYonetici([FromBody] RegisterAdminDto dto)
        {
            var yeni = new Kullanici
            {
                Id = IdGenerator.CreateV7(),
                Ad = dto.Ad,
                Soyad = dto.Soyad,
                EPosta = dto.EPosta,
                SifreHash = BCrypt.Net.BCrypt.HashPassword(dto.Sifre),
                Rol = KullaniciRol.Admin,
                MusteriId = Guid.Empty, // Sistem adminleri MusteriId'den bağımsız olabilir veya Guid.Empty kullanılabilir
                AktifMi = true
            };

            _context.Kullanicilar.Add(yeni);
            await _context.SaveChangesAsync();

            return Ok(new { mesaj = "Sistem yöneticisi başarıyla eklendi." });
        }
    }
}
