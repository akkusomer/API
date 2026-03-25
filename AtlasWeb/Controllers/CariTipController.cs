using AtlasWeb.Data;
using AtlasWeb.DTOs;
using AtlasWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AtlasWeb.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CariTipController : ControllerBase
    {
        private readonly AtlasDbContext _context;

        public CariTipController(AtlasDbContext context) => _context = context;

        /// <summary>Tüm aktif cari tipleri listeler (tüm kullanıcılar görebilir).</summary>
        [HttpGet]
        public async Task<IActionResult> GetCariTipler()
        {
            var tipler = await _context.CariTipler
                .OrderBy(ct => ct.Adi)
                .Select(ct => new
                {
                    ct.Id,
                    ct.Adi,
                    ct.Aciklama,
                    ct.KayitTarihi
                })
                .ToListAsync();

            return Ok(tipler);
        }

        /// <summary>Tekil cari tip detayı.</summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var tip = await _context.CariTipler.FindAsync(id);
            if (tip == null) return NotFound(new { hata = "Cari tip bulunamadı." });
            return Ok(tip);
        }

        /// <summary>Yeni cari tip ekler — sadece Admin.</summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Ekle([FromBody] CariTipDto dto)
        {
            // Aynı isimde aktif tip var mı?
            var mevcutMu = await _context.CariTipler
                .AnyAsync(ct => ct.Adi.ToLower() == dto.Adi.ToLower());

            if (mevcutMu)
                return BadRequest(new { hata = "Bu isimde bir cari tip zaten mevcut." });

            var yeniTip = new CariTip
            {
                Adi      = dto.Adi,
                Aciklama = dto.Aciklama
            };

            _context.CariTipler.Add(yeniTip);
            await _context.SaveChangesAsync();

            return Ok(new { mesaj = "Cari tip başarıyla eklendi.", id = yeniTip.Id });
        }

        /// <summary>Cari tipi günceller — sadece Admin.</summary>
        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Guncelle(Guid id, [FromBody] CariTipDto dto)
        {
            var tip = await _context.CariTipler.FindAsync(id);
            if (tip == null) return NotFound(new { hata = "Cari tip bulunamadı." });

            // İsim çakışması kontrolü (kendisi hariç)
            var cakisma = await _context.CariTipler
                .AnyAsync(ct => ct.Adi.ToLower() == dto.Adi.ToLower() && ct.Id != id);

            if (cakisma)
                return BadRequest(new { hata = "Bu isimde başka bir cari tip zaten mevcut." });

            tip.Adi      = dto.Adi;
            tip.Aciklama = dto.Aciklama;

            await _context.SaveChangesAsync();
            return Ok(new { mesaj = "Cari tip güncellendi." });
        }

        /// <summary>Cari tipi pasife çeker (soft delete) — sadece Admin.</summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Sil(Guid id)
        {
            var tip = await _context.CariTipler.FindAsync(id);
            if (tip == null) return NotFound(new { hata = "Cari tip bulunamadı." });

            // Bu tipe bağlı aktif cari kart var mı?
            var bagliKartVarMi = await _context.CariKartlar
                .IgnoreQueryFilters()
                .AnyAsync(ck => ck.CariTipId == id && ck.AktifMi);

            if (bagliKartVarMi)
                return Conflict(new { hata = "Bu cari tipe bağlı aktif cari kartlar bulunmaktadır. Önce kartları silin veya başka bir tipe taşıyın." });

            _context.CariTipler.Remove(tip); // ISoftDelete → SaveChangesAsync soft-delete uygular
            await _context.SaveChangesAsync();

            return Ok(new { mesaj = "Cari tip pasife alındı." });
        }
    }
}
