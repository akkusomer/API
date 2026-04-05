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
    public class KasaFisController : ControllerBase
    {
        private readonly AtlasDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public KasaFisController(AtlasDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        [HttpGet]
        public async Task<IActionResult> GetList(
            [FromQuery] int sayfa = 1,
            [FromQuery] int sayfaBoyutu = 50,
            [FromQuery] string? arama = null)
        {
            sayfaBoyutu = Math.Min(sayfaBoyutu, 500);

            var query = _context.KasaFisleri
                .Include(kf => kf.CariKart)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(arama))
            {
                var term = arama.Trim().ToLower();
                query = query.Where(kf =>
                    kf.BelgeNo.ToString().Contains(term)
                    || kf.KasaAdi.ToLower().Contains(term)
                    || (kf.Aciklama1 != null && kf.Aciklama1.ToLower().Contains(term))
                    || (kf.CariKart != null && (
                        (kf.CariKart.Unvan != null && kf.CariKart.Unvan.ToLower().Contains(term))
                        || (kf.CariKart.AdiSoyadi != null && kf.CariKart.AdiSoyadi.ToLower().Contains(term))
                    )));
            }

            var toplamKayit = await query.CountAsync();

            var veriler = await query
                .OrderByDescending(kf => kf.Tarih)
                .ThenByDescending(kf => kf.KayitTarihi)
                .Skip((sayfa - 1) * sayfaBoyutu)
                .Take(sayfaBoyutu)
                .Select(kf => new
                {
                    kf.Id,
                    kf.KasaAdi,
                    kf.BelgeKodu,
                    kf.BelgeNo,
                    kf.IslemTipi,
                    IslemTipiAdi = kf.IslemTipi == KasaIslemTipi.Tahsilat ? "Tahsilat (Giris)" : "Odeme (Cikis)",
                    kf.CariKartId,
                    CariAdi = kf.CariKart != null
                        ? (kf.CariKart.Unvan ?? kf.CariKart.AdiSoyadi)
                        : null,
                    kf.Tarih,
                    kf.HareketTipi,
                    kf.Aciklama1,
                    kf.Aciklama2,
                    kf.Pos,
                    kf.OzelKodu,
                    kf.Tutar
                })
                .ToListAsync();

            return Ok(new
            {
                veriler,
                toplamKayit,
                mevcutSayfa = sayfa,
                sayfaBoyutu,
                toplamSayfa = (int)Math.Ceiling(toplamKayit / (double)sayfaBoyutu)
            });
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var fis = await _context.KasaFisleri
                .Include(kf => kf.CariKart)
                .FirstOrDefaultAsync(kf => kf.Id == id);

            if (fis is null)
            {
                return NotFound(new { hata = "Kasa fisi bulunamadi." });
            }

            if (!_currentUserService.IsSystemAdmin && fis.MusteriId != _currentUserService.MusteriId)
            {
                return Forbid();
            }

            return Ok(new
            {
                fis.Id,
                fis.KasaAdi,
                fis.BelgeKodu,
                fis.BelgeNo,
                fis.IslemTipi,
                fis.CariKartId,
                CariAdi = fis.CariKart != null
                    ? (fis.CariKart.Unvan ?? fis.CariKart.AdiSoyadi)
                    : null,
                fis.Tarih,
                fis.HareketTipi,
                fis.Aciklama1,
                fis.Aciklama2,
                fis.Pos,
                fis.OzelKodu,
                fis.Tutar
            });
        }

        [HttpPost]
        public async Task<IActionResult> Ekle([FromBody] KasaFisDto dto)
        {
            if (_currentUserService.MusteriId is null || _currentUserService.MusteriId == Guid.Empty)
            {
                return Unauthorized(new { hata = "Kasa fisi acabilmek icin bir sirkete bagli olmalisiniz." });
            }

            var musteriId = _currentUserService.MusteriId.Value;
            var validation = await ValidateDtoAsync(musteriId, dto);
            if (validation is not null)
            {
                return validation;
            }

            var belgeNo = await GenerateNextBelgeNoAsync(musteriId);

            var fis = new KasaFis
            {
                MusteriId = musteriId,
                KasaAdi = NormalizeWithDefault(dto.KasaAdi, "MERKEZ TL KASA", 100),
                BelgeKodu = NormalizeWithDefault(dto.BelgeKodu, "KF", 10),
                BelgeNo = belgeNo,
                IslemTipi = dto.IslemTipi,
                CariKartId = dto.CariKartId,
                Tarih = NormalizeDate(dto.Tarih),
                OzelKodu = NormalizeOptional(dto.OzelKodu, 50),
                HareketTipi = NormalizeWithDefault(dto.HareketTipi, "GENEL", 50),
                Aciklama1 = NormalizeOptional(dto.Aciklama1, 200),
                Aciklama2 = NormalizeOptional(dto.Aciklama2, 200),
                Pos = NormalizeOptional(dto.Pos, 50),
                Tutar = NormalizeMoney(dto.Tutar)
            };

            _context.KasaFisleri.Add(fis);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                mesaj = "Kasa fisi basariyla olusturuldu.",
                id = fis.Id,
                belgeNo = fis.BelgeNo
            });
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Guncelle(Guid id, [FromBody] KasaFisDto dto)
        {
            var fis = await _context.KasaFisleri.FirstOrDefaultAsync(kf => kf.Id == id);
            if (fis is null)
            {
                return NotFound(new { hata = "Kasa fisi bulunamadi." });
            }

            if (!_currentUserService.IsSystemAdmin && fis.MusteriId != _currentUserService.MusteriId)
            {
                return Forbid();
            }

            var validation = await ValidateDtoAsync(fis.MusteriId, dto);
            if (validation is not null)
            {
                return validation;
            }

            fis.KasaAdi = NormalizeWithDefault(dto.KasaAdi, fis.KasaAdi, 100);
            fis.BelgeKodu = NormalizeWithDefault(dto.BelgeKodu, fis.BelgeKodu, 10);
            fis.IslemTipi = dto.IslemTipi;
            fis.CariKartId = dto.CariKartId;
            fis.Tarih = NormalizeDate(dto.Tarih);
            fis.OzelKodu = NormalizeOptional(dto.OzelKodu, 50);
            fis.HareketTipi = NormalizeWithDefault(dto.HareketTipi, "GENEL", 50);
            fis.Aciklama1 = NormalizeOptional(dto.Aciklama1, 200);
            fis.Aciklama2 = NormalizeOptional(dto.Aciklama2, 200);
            fis.Pos = NormalizeOptional(dto.Pos, 50);
            fis.Tutar = NormalizeMoney(dto.Tutar);

            await _context.SaveChangesAsync();

            return Ok(new { mesaj = "Kasa fisi guncellendi." });
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Sil(Guid id)
        {
            var fis = await _context.KasaFisleri.FirstOrDefaultAsync(kf => kf.Id == id);
            if (fis is null)
            {
                return NotFound(new { hata = "Kasa fisi bulunamadi." });
            }

            if (!_currentUserService.IsSystemAdmin && fis.MusteriId != _currentUserService.MusteriId)
            {
                return Forbid();
            }

            _context.KasaFisleri.Remove(fis);
            await _context.SaveChangesAsync();
            return Ok(new { mesaj = "Kasa fisi silindi." });
        }

        private async Task<IActionResult?> ValidateDtoAsync(Guid musteriId, KasaFisDto dto)
        {
            if (dto.Tutar <= 0)
            {
                return BadRequest(new { hata = "Tutar 0'dan buyuk olmalidir." });
            }

            if (dto.CariKartId is not null && dto.CariKartId != Guid.Empty)
            {
                var cariExists = await _context.CariKartlar
                    .IgnoreQueryFilters()
                    .AnyAsync(c => c.Id == dto.CariKartId && c.MusteriId == musteriId && c.AktifMi);

                if (!cariExists)
                {
                    return BadRequest(new { hata = "Secilen cari kart bulunamadi." });
                }
            }

            return null;
        }

        private async Task<int> GenerateNextBelgeNoAsync(Guid musteriId)
        {
            var maxValue = await _context.KasaFisleri
                .IgnoreQueryFilters()
                .Where(kf => kf.MusteriId == musteriId)
                .MaxAsync(kf => (int?)kf.BelgeNo);

            return (maxValue ?? 0) + 1;
        }

        private static DateTime NormalizeDate(DateTime value)
        {
            var date = value == default ? DateTime.UtcNow.Date : value.Date;
            return DateTime.SpecifyKind(date, DateTimeKind.Utc);
        }

        private static decimal NormalizeMoney(decimal value)
        {
            return Math.Round(Math.Max(0, value), 2, MidpointRounding.AwayFromZero);
        }

        private static string? NormalizeOptional(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var trimmed = value.Trim();
            return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
        }

        private static string NormalizeWithDefault(string? value, string fallback, int maxLength)
        {
            var normalized = NormalizeOptional(value, maxLength);
            return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
        }
    }
}
