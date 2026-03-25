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

        public MusteriController(AtlasDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetMusteriler()
        {
            // Admin tüm müşterileri; diğer kullanıcılar yalnızca bağlı oldukları kiracıyı görür (HasQueryFilter).
            var musteriler = await _context.Musteriler.ToListAsync();
            return Ok(musteriler);
        }

        [HttpGet("silinenler-dahil")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetTumMusterilerSilinenlerDahil()
        {
            // Admin için global filtreyi deliyoruz
            var tumKayitlar = await _context.Musteriler.IgnoreQueryFilters().ToListAsync();
            return Ok(tumKayitlar);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateMusteri([FromBody] MusteriDto dto)
        {
            if (await _context.Musteriler.AnyAsync(m => m.MusteriKodu == dto.MusteriKodu))
                return BadRequest(new { hata = "Bu müşteri kodu zaten kullanımda." });

            if (!string.IsNullOrEmpty(dto.VergiNo) && await _context.Musteriler.AnyAsync(m => m.VergiNo == dto.VergiNo))
                return BadRequest(new { hata = "Bu VKN/TCKN ile kayıtlı başka bir müşteri var." });

            var yeni = new Musteri
            {
                Id = IdGenerator.CreateV7(),
                MusteriKodu = dto.MusteriKodu,
                Unvan = dto.Unvan,
                VergiNo = dto.VergiNo,
                VergiDairesi = dto.VergiDairesi,
                KimlikTuru = dto.KimlikTuru,
                GsmNo = dto.GsmNo,
                EPosta = dto.EPosta,
                Il = dto.Il,
                Ilce = dto.Ilce,
                AdresDetay = dto.AdresDetay,
                PaketTipi = dto.PaketTipi,
                AktifMi = dto.AktifMi,
                KayitTarihi = DateTime.UtcNow
            };

            _context.Musteriler.Add(yeni);
            await _context.SaveChangesAsync();

            return Ok(yeni);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateMusteri(Guid id, [FromBody] MusteriDto dto)
        {
            var musteri = await _context.Musteriler.FindAsync(id);
            if (musteri == null) return NotFound();

            musteri.Unvan = dto.Unvan;
            musteri.MusteriKodu = dto.MusteriKodu;
            musteri.VergiNo = dto.VergiNo;
            musteri.VergiDairesi = dto.VergiDairesi;
            musteri.KimlikTuru = dto.KimlikTuru;
            musteri.GsmNo = dto.GsmNo;
            musteri.EPosta = dto.EPosta;
            musteri.Il = dto.Il;
            musteri.Ilce = dto.Ilce;
            musteri.AdresDetay = dto.AdresDetay;
            musteri.PaketTipi = dto.PaketTipi;
            musteri.AktifMi = dto.AktifMi;

            await _context.SaveChangesAsync();
            return Ok(musteri);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> SoftDelete(Guid id)
        {
            var musteri = await _context.Musteriler.FindAsync(id);
            if (musteri == null) return NotFound(new { hata = "Belirtilen müşteri bulunamadı." });

            // SaveChanges: BaseEntity olmayan ISoftDelete kayıtları için pasifleştirme (Musteri).
            _context.Musteriler.Remove(musteri);
            await _context.SaveChangesAsync();

            return Ok(new { mesaj = "Müşteri kaydı başarıyla silinmiş (pasife alınmış) olarak işaretlendi." });
        }

        [HttpDelete("{id}/hard")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> HardDelete(Guid id)
        {
            // ExecuteDeleteAsync veritabanına direkt DELETE FROM sorgusu atar, EF Core ChangeTracker'ı atlar (Kalıcı Silme).
            var silinenKayit = await _context.Musteriler.Where(m => m.Id == id).ExecuteDeleteAsync();
            
            if (silinenKayit == 0) return NotFound(new { hata = "Silinecek müşteri bulunamadı." });

            return Ok(new { mesaj = "Müşteri kaydı veritabanından kalıcı olarak (Hard Delete) silindi." });
        }
    }
}