using AtlasWeb.Data;
using AtlasWeb.DTOs;
using AtlasWeb.Models;
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
        private readonly ICurrentUserService _currentUserService;
        private readonly IHksIlService _hksIlService;
        private readonly IHksIlceService _hksIlceService;
        private readonly IHksBeldeService _hksBeldeService;

        public AdminController(
            AtlasDbContext context,
            ICurrentUserService currentUserService,
            IHksIlService hksIlService,
            IHksIlceService hksIlceService,
            IHksBeldeService hksBeldeService)
        {
            _context = context;
            _currentUserService = currentUserService;
            _hksIlService = hksIlService;
            _hksIlceService = hksIlceService;
            _hksBeldeService = hksBeldeService;
        }

        [HttpGet("aktiviteler")]
        public async Task<IActionResult> GetUserActivities()
        {
            var denial = EnsureSystemAdmin();
            if (denial is not null)
            {
                return denial;
            }

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
            var denial = EnsureSystemAdmin();
            if (denial is not null)
            {
                return denial;
            }

            var hatalar = await _context.ErrorLogs
                .OrderByDescending(x => x.Tarih)
                .Take(100)
                .ToListAsync();

            return Ok(hatalar);
        }

        [HttpGet("dosya-loglari")]
        public async Task<IActionResult> GetFileLogs()
        {
            var denial = EnsureSystemAdmin();
            if (denial is not null)
            {
                return denial;
            }

            try
            {
                var logPath = Path.Combine(AppContext.BaseDirectory, "logs");
                if (!Directory.Exists(logPath))
                {
                    return NotFound("Log dizini bulunamadi.");
                }

                var file = Directory.GetFiles(logPath)
                    .OrderByDescending(f => f)
                    .FirstOrDefault();

                if (file is null)
                {
                    return NotFound("Log dosyasi bulunamadi.");
                }

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
            var denial = EnsureSystemAdmin();
            if (denial is not null)
            {
                return denial;
            }

            var users = await _context.Kullanicilar
                .IgnoreQueryFilters()
                .Where(u => u.MusteriId == id)
                .Select(u => new
                {
                    u.Id,
                    u.Ad,
                    u.Soyad,
                    u.EPosta,
                    u.Telefon,
                    u.Rol,
                    u.AktifMi
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpPost("musteri/{id}/kullanici")]
        public async Task<IActionResult> AddUserToMusteri(Guid id, [FromBody] RegisterUserDto dto)
        {
            var denial = EnsureSystemAdmin();
            if (denial is not null)
            {
                return denial;
            }

            var musteriVarMi = await _context.Musteriler
                .IgnoreQueryFilters()
                .AnyAsync(m => m.Id == id && m.AktifMi);

            if (!musteriVarMi)
            {
                return BadRequest("Musteri bulunamadi.");
            }

            var normalizedEmail = IdentityNormalizer.NormalizeEmail(dto.EPosta);
            var emailExists = await _context.Kullanicilar
                .IgnoreQueryFilters()
                .AnyAsync(u => u.EPosta == normalizedEmail);

            if (emailExists)
            {
                return Conflict(new { hata = "Bu e-posta zaten kayitli." });
            }

            var yeni = new Kullanici
            {
                Id = IdGenerator.CreateV7(),
                Ad = dto.Ad.Trim(),
                Soyad = dto.Soyad.Trim(),
                EPosta = normalizedEmail,
                Telefon = NormalizePhone(dto.Telefon),
                SifreHash = BCrypt.Net.BCrypt.HashPassword(dto.Sifre),
                Rol = KullaniciRol.User,
                MusteriId = id,
                AktifMi = true
            };

            _context.Kullanicilar.Add(yeni);
            await _context.SaveChangesAsync();

            return Ok(new { mesaj = "Kullanici basariyla eklendi." });
        }

        [HttpDelete("kullanici/{id}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            var denial = EnsureSystemAdmin();
            if (denial is not null)
            {
                return denial;
            }

            var user = await _context.Kullanicilar
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user is null)
            {
                return NotFound(new { mesaj = "Kullanici bulunamadi." });
            }

            _context.Kullanicilar.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(new { mesaj = "Kullanici pasife alindi." });
        }

        [HttpGet("yoneticiler")]
        public async Task<IActionResult> GetYoneticiler()
        {
            var denial = EnsureSystemAdmin();
            if (denial is not null)
            {
                return denial;
            }

            var users = await _context.Kullanicilar
                .IgnoreQueryFilters()
                .Where(u => u.Rol == KullaniciRol.Admin)
                .Select(u => new
                {
                    u.Id,
                    u.Ad,
                    u.Soyad,
                    u.EPosta,
                    u.Telefon,
                    u.Rol,
                    u.AktifMi
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpPost("yonetici")]
        public async Task<IActionResult> AddYonetici([FromBody] RegisterAdminDto dto)
        {
            var denial = EnsureSystemAdmin();
            if (denial is not null)
            {
                return denial;
            }

            var normalizedEmail = IdentityNormalizer.NormalizeEmail(dto.EPosta);
            var emailExists = await _context.Kullanicilar
                .IgnoreQueryFilters()
                .AnyAsync(u => u.EPosta == normalizedEmail);

            if (emailExists)
            {
                return Conflict(new { hata = "Bu e-posta zaten kayitli." });
            }

            var yeni = new Kullanici
            {
                Id = IdGenerator.CreateV7(),
                Ad = dto.Ad.Trim(),
                Soyad = dto.Soyad.Trim(),
                EPosta = normalizedEmail,
                Telefon = NormalizePhone(dto.Telefon),
                SifreHash = BCrypt.Net.BCrypt.HashPassword(dto.Sifre),
                Rol = KullaniciRol.Admin,
                MusteriId = AtlasDbContext.SystemMusteriId,
                AktifMi = true
            };

            _context.Kullanicilar.Add(yeni);
            await _context.SaveChangesAsync();

            return Ok(new { mesaj = "Sistem yoneticisi basariyla eklendi." });
        }

        [HttpPut("yonetici/{id:guid}")]
        public async Task<IActionResult> UpdateYonetici(Guid id, [FromBody] UpdateAdminDto dto)
        {
            var denial = EnsureSystemAdmin();
            if (denial is not null)
            {
                return denial;
            }

            var yonetici = await _context.Kullanicilar
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Id == id && u.Rol == KullaniciRol.Admin);

            if (yonetici is null)
            {
                return NotFound(new { hata = "Sistem yoneticisi bulunamadi." });
            }

            yonetici.Ad = dto.Ad.Trim();
            yonetici.Soyad = dto.Soyad.Trim();
            yonetici.Telefon = NormalizePhone(dto.Telefon);

            await _context.SaveChangesAsync();
            return Ok(new { mesaj = "Sistem yoneticisi guncellendi." });
        }

        [HttpPost("hks/iller/tum-sirketler/{sourceTenantId:guid}")]
        public async Task<IActionResult> SyncHksCitiesForAllCustomers(Guid sourceTenantId, CancellationToken cancellationToken)
        {
            var denial = EnsureSystemAdmin();
            if (denial is not null)
            {
                return denial;
            }

            if (sourceTenantId == Guid.Empty || sourceTenantId == AtlasDbContext.SystemMusteriId)
            {
                return BadRequest(new { hata = "HKS il listesi icin gecerli bir sirket secin." });
            }

            var sourceCustomerExists = await _context.Musteriler
                .IgnoreQueryFilters()
                .AnyAsync(x => x.Id == sourceTenantId && x.AktifMi, cancellationToken);

            if (!sourceCustomerExists)
            {
                return NotFound(new { hata = "Secili sirket bulunamadi." });
            }

            var result = await _hksIlService.SyncCitiesForAllTenantsAsync(sourceTenantId, cancellationToken);
            return Ok(result);
        }

        [HttpPost("hks/ilceler/tum-sirketler/{sourceTenantId:guid}")]
        public async Task<IActionResult> SyncHksDistrictsForAllCustomers(Guid sourceTenantId, CancellationToken cancellationToken)
        {
            var denial = EnsureSystemAdmin();
            if (denial is not null)
            {
                return denial;
            }

            if (sourceTenantId == Guid.Empty || sourceTenantId == AtlasDbContext.SystemMusteriId)
            {
                return BadRequest(new { hata = "HKS ilce listesi icin gecerli bir sirket secin." });
            }

            var sourceCustomerExists = await _context.Musteriler
                .IgnoreQueryFilters()
                .AnyAsync(x => x.Id == sourceTenantId && x.AktifMi, cancellationToken);

            if (!sourceCustomerExists)
            {
                return NotFound(new { hata = "Secili sirket bulunamadi." });
            }

            var result = await _hksIlceService.SyncDistrictsForAllTenantsAsync(sourceTenantId, cancellationToken);
            return Ok(result);
        }

        [HttpPost("hks/beldeler/tum-sirketler/{sourceTenantId:guid}")]
        public async Task<IActionResult> SyncHksTownsForAllCustomers(Guid sourceTenantId, CancellationToken cancellationToken)
        {
            var denial = EnsureSystemAdmin();
            if (denial is not null)
            {
                return denial;
            }

            if (sourceTenantId == Guid.Empty || sourceTenantId == AtlasDbContext.SystemMusteriId)
            {
                return BadRequest(new { hata = "HKS belde listesi icin gecerli bir sirket secin." });
            }

            var sourceCustomerExists = await _context.Musteriler
                .IgnoreQueryFilters()
                .AnyAsync(x => x.Id == sourceTenantId && x.AktifMi, cancellationToken);

            if (!sourceCustomerExists)
            {
                return NotFound(new { hata = "Secili sirket bulunamadi." });
            }

            var result = await _hksBeldeService.SyncTownsForAllTenantsAsync(sourceTenantId, cancellationToken);
            return Ok(result);
        }

        private IActionResult? EnsureSystemAdmin() => _currentUserService.IsSystemAdmin ? null : Forbid();

        private static string? NormalizePhone(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
