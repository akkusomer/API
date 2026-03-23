using AtlasWeb.Data;
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
            // Global Filter sayesinde herkes sadece kendi datasını görür
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

        [HttpDelete("{id}")]
        public async Task<IActionResult> SoftDelete(Guid id)
        {
            var musteri = await _context.Musteriler.FindAsync(id);
            if (musteri == null) return NotFound(new { hata = "Belirtilen müşteri bulunamadı." });

            // Remove tetiklendiğinde AtlasDbContext içindeki interceptor sayesinde Active = false (Soft Delete) yapılır.
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