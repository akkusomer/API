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
    public class StokController : ControllerBase
    {
        private readonly AtlasDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public StokController(AtlasDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        [HttpGet]
        public async Task<IActionResult> GetStoklar([FromQuery] int sayfa = 1, [FromQuery] int sayfaBoyutu = 20)
        {
            sayfaBoyutu = Math.Min(sayfaBoyutu, 100); // Maksimum 100 kayıt dönsün
            var stoklar = await _context.Stoklar
                .Include(s => s.Birim)
                .OrderByDescending(s => s.KayitTarihi)
                .Skip((sayfa - 1) * sayfaBoyutu)
                .Take(sayfaBoyutu)
                .Select(s => new {
                    s.Id,
                    s.StokKodu,
                    s.StokAdi,
                    s.YedekAdi,
                    BirimAdi = s.Birim.Ad,
                    BirimSembolu = s.Birim.Sembol
                })
                .ToListAsync();

            var toplamKayit = await _context.Stoklar.CountAsync();

            return Ok(new {
                veriler = stoklar,
                toplamKayit,
                mevcutSayfa = sayfa,
                sayfaBoyutu,
                toplamSayfa = (int)Math.Ceiling(toplamKayit / (double)sayfaBoyutu)
            });
        }

        [HttpPost]
        public async Task<IActionResult> Ekle([FromBody] StokDto dto)
        {
            // Güvenlik ve Tenant İzolasyonu
            if (_currentUserService.MusteriId == null || _currentUserService.MusteriId == Guid.Empty)
                return Unauthorized(new { hata = "Stok açabilmek için öncelikle bir şirkete bağlı olmanız gerekmektedir (Müşteri ID bulunamadı)." });

            var musteriId = _currentUserService.MusteriId.Value;

            // Ölçü biriminin sistemde var olup olmadığının teyitsel kontrolü
            var birimVarMi = await _context.Birimler.AnyAsync(b => b.Id == dto.BirimId);
            if (!birimVarMi) return BadRequest(new { hata = "Seçilen ölçü birimi sistemde bulunamadı." });

            // Race Condition Önleyici: Serializable transaction ile aynı anda gelen isteklerin aynı kodu alması engellendi
            using var transaction = await _context.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable);
            try
            {
                var kodlar = await _context.Stoklar
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(s => s.MusteriId == musteriId)
                    .Select(s => s.StokKodu)
                    .ToListAsync();

                var siradakiNo = kodlar.Count == 0
                    ? 1
                    : kodlar.Select(k => int.TryParse(k, out var n) ? n : 0).Max() + 1;

                var yeniStok = new Stok
                {
                    MusteriId = musteriId,
                    StokKodu = siradakiNo.ToString("D5"),
                    StokAdi = dto.StokAdi,
                    YedekAdi = dto.YedekAdi,
                    BirimId = dto.BirimId
                };

                _context.Stoklar.Add(yeniStok);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { mesaj = "Stok kartı başarıyla açıldı.", stokKodu = yeniStok.StokKodu });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Conflict(new { hata = "Stok kodu üretiminde çakışma meydana geldi.", detay = ex.Message, inner = ex.InnerException?.Message });
            }
        }
    }
}
