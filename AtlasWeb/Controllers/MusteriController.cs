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
    [Authorize]
    public class MusteriController : ControllerBase
    {
        private readonly AtlasDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public MusteriController(AtlasDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        [HttpGet]
        public async Task<IActionResult> GetMusteriler()
        {
            var musteriler = await _context.Musteriler.ToListAsync();
            return Ok(musteriler);
        }

        [HttpGet("silinenler-dahil")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetTumMusterilerSilinenlerDahil()
        {
            if (!_currentUserService.IsSystemAdmin)
            {
                return Forbid();
            }

            var tumKayitlar = await _context.Musteriler.IgnoreQueryFilters().ToListAsync();
            return Ok(tumKayitlar);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateMusteri([FromBody] MusteriDto dto)
        {
            if (!_currentUserService.IsSystemAdmin)
            {
                return Forbid();
            }

            var kodExists = await _context.Musteriler
                .IgnoreQueryFilters()
                .AnyAsync(m => m.MusteriKodu == dto.MusteriKodu);

            if (kodExists)
            {
                return BadRequest(new { hata = "Bu musteri kodu zaten kullanimda." });
            }

            if (!string.IsNullOrWhiteSpace(dto.VergiNo))
            {
                var vergiExists = await _context.Musteriler
                    .IgnoreQueryFilters()
                    .AnyAsync(m => m.VergiNo == dto.VergiNo);

                if (vergiExists)
                {
                    return BadRequest(new { hata = "Bu VKN/TCKN ile kayitli baska bir musteri var." });
                }
            }

            var yeni = new Musteri
            {
                Id = IdGenerator.CreateV7(),
                MusteriKodu = dto.MusteriKodu.Trim(),
                Unvan = dto.Unvan.Trim(),
                VergiNo = dto.VergiNo.Trim(),
                VergiDairesi = dto.VergiDairesi.Trim(),
                KimlikTuru = dto.KimlikTuru,
                GsmNo = dto.GsmNo.Trim(),
                EPosta = IdentityNormalizer.NormalizeEmail(dto.EPosta),
                Il = dto.Il.Trim(),
                Ilce = dto.Ilce.Trim(),
                AdresDetay = dto.AdresDetay.Trim(),
                PaketTipi = dto.PaketTipi,
                AktifMi = dto.AktifMi,
                KayitTarihi = DateTime.UtcNow
            };

            _context.Musteriler.Add(yeni);
            await _context.SaveChangesAsync();
            await TenantReferenceDataSeeder.EnsureDefaultsForCustomerAsync(_context, yeni.Id);

            return Ok(yeni);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateMusteri(Guid id, [FromBody] MusteriDto dto)
        {
            if (!_currentUserService.IsSystemAdmin)
            {
                return Forbid();
            }

            if (id == AtlasDbContext.SystemMusteriId && !dto.AktifMi)
            {
                return BadRequest(new { hata = "Sistem sirketi pasif yapilamaz." });
            }

            var musteri = await _context.Musteriler
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (musteri is null)
            {
                return NotFound();
            }

            var kodExists = await _context.Musteriler
                .IgnoreQueryFilters()
                .AnyAsync(m => m.MusteriKodu == dto.MusteriKodu && m.Id != id);

            if (kodExists)
            {
                return BadRequest(new { hata = "Bu musteri kodu zaten kullanimda." });
            }

            if (!string.IsNullOrWhiteSpace(dto.VergiNo))
            {
                var vergiExists = await _context.Musteriler
                    .IgnoreQueryFilters()
                    .AnyAsync(m => m.VergiNo == dto.VergiNo && m.Id != id);

                if (vergiExists)
                {
                    return BadRequest(new { hata = "Bu VKN/TCKN ile kayitli baska bir musteri var." });
                }
            }

            musteri.Unvan = dto.Unvan.Trim();
            musteri.MusteriKodu = dto.MusteriKodu.Trim();
            musteri.VergiNo = dto.VergiNo.Trim();
            musteri.VergiDairesi = dto.VergiDairesi.Trim();
            musteri.KimlikTuru = dto.KimlikTuru;
            musteri.GsmNo = dto.GsmNo.Trim();
            musteri.EPosta = IdentityNormalizer.NormalizeEmail(dto.EPosta);
            musteri.Il = dto.Il.Trim();
            musteri.Ilce = dto.Ilce.Trim();
            musteri.AdresDetay = dto.AdresDetay.Trim();
            musteri.PaketTipi = dto.PaketTipi;
            musteri.AktifMi = id == AtlasDbContext.SystemMusteriId || dto.AktifMi;

            await _context.SaveChangesAsync();
            return Ok(musteri);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SoftDelete(Guid id)
        {
            if (!_currentUserService.IsSystemAdmin)
            {
                return Forbid();
            }

            if (id == AtlasDbContext.SystemMusteriId)
            {
                return BadRequest(new { hata = "Sistem sirketi pasife alinamaz." });
            }

            var musteri = await _context.Musteriler
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (musteri is null)
            {
                return NotFound(new { hata = "Belirtilen musteri bulunamadi." });
            }

            _context.Musteriler.Remove(musteri);
            await _context.SaveChangesAsync();

            return Ok(new { mesaj = "Musteri pasif duruma getirildi." });
        }

        [HttpDelete("{id}/hard")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> HardDelete(Guid id)
        {
            if (!_currentUserService.IsSystemAdmin)
            {
                return Forbid();
            }

            if (id == AtlasDbContext.SystemMusteriId)
            {
                return BadRequest(new { hata = "Sistem sirketi kalici olarak silinemez." });
            }

            var relatedCounts = new Dictionary<string, int>
            {
                ["kullanicilar"] = await _context.Kullanicilar.IgnoreQueryFilters().CountAsync(x => x.MusteriId == id),
                ["stoklar"] = await _context.Stoklar.IgnoreQueryFilters().CountAsync(x => x.MusteriId == id),
                ["cariKartlar"] = await _context.CariKartlar.IgnoreQueryFilters().CountAsync(x => x.MusteriId == id),
                ["cariTipler"] = await _context.CariTipler.IgnoreQueryFilters().CountAsync(x => x.MusteriId == id),
                ["birimler"] = await _context.Birimler.IgnoreQueryFilters().CountAsync(x => x.MusteriId == id),
                ["faturalar"] = await _context.Faturalar.IgnoreQueryFilters().CountAsync(x => x.MusteriId == id)
            };

            if (relatedCounts.Any(item => item.Value > 0))
            {
                return Conflict(new
                {
                    hata = "Musteri iliskili kayitlar bulundugu icin kalici olarak silinemiyor.",
                    iliskiliKayitlar = relatedCounts.Where(item => item.Value > 0)
                });
            }

            var silinenKayit = await _context.Musteriler
                .IgnoreQueryFilters()
                .Where(m => m.Id == id)
                .ExecuteDeleteAsync();

            if (silinenKayit == 0)
            {
                return NotFound(new { hata = "Silinecek musteri bulunamadi." });
            }

            return Ok(new { mesaj = "Musteri kalici olarak silindi." });
        }

        [HttpPost("{id:guid}/varsayilan-tanimlar")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EnsureDefaultDefinitions(Guid id)
        {
            if (!_currentUserService.IsSystemAdmin)
            {
                return Forbid();
            }

            var musteriVarMi = await _context.Musteriler
                .IgnoreQueryFilters()
                .AnyAsync(m => m.Id == id && m.AktifMi);

            if (!musteriVarMi)
            {
                return NotFound(new { hata = "Musteri bulunamadi." });
            }

            await TenantReferenceDataSeeder.EnsureDefaultsForCustomerAsync(_context, id);

            var birimSayisi = await _context.Birimler
                .IgnoreQueryFilters()
                .CountAsync(b => b.MusteriId == id && b.AktifMi);

            var cariTipSayisi = await _context.CariTipler
                .IgnoreQueryFilters()
                .CountAsync(ct => ct.MusteriId == id && ct.AktifMi);

            return Ok(new
            {
                mesaj = "Varsayilan birim ve cari tip tanimlari guncellendi.",
                birimSayisi,
                cariTipSayisi
            });
        }
    }
}
