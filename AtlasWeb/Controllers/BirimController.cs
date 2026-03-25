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
    [Authorize] // Sadece giriş yapmış kullanıcılar verileri okuyabilir
    public class BirimController : ControllerBase
    {
        private readonly AtlasDbContext _context;

        public BirimController(AtlasDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetBirimler()
        {
            var birimler = await _context.Birimler
                .OrderBy(b => b.Ad)
                .ToListAsync();
            return Ok(birimler);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")] // Yalnızca admin ekleyebilir
        public async Task<IActionResult> Ekle([FromBody] BirimDto dto)
        {
            if (await _context.Birimler.AnyAsync(b => b.Ad.ToLower() == dto.Ad.ToLower() || b.Sembol.ToLower() == dto.Sembol.ToLower()))
                return BadRequest(new { hata = "Bu ölçü birimi veya sembol zaten mevcut." });

            var yeniBirim = new Birim
            {
                Ad = dto.Ad,
                Sembol = dto.Sembol
            };

            _context.Birimler.Add(yeniBirim);
            await _context.SaveChangesAsync();

            return Ok(new { mesaj = "Ölçü birimi başarıyla eklendi." });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")] // Yalnızca admin silebilir
        public async Task<IActionResult> Sil(Guid id)
        {
            var birim = await _context.Birimler.FindAsync(id);
            if (birim == null) return NotFound(new { hata = "Ölçü birimi bulunamadı." });

            _context.Birimler.Remove(birim); // SaveChanges: ISoftDelete (Birim) için soft delete
            await _context.SaveChangesAsync();

            return Ok(new { mesaj = "Ölçü birimi sistemden başarıyla silindi (pasife çekildi)." });
        }
    }
}
