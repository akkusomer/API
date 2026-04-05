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
        private static readonly string[] HksNitelikSecenekleri = ["Yerli", "İthal"];
        private readonly AtlasDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public StokController(AtlasDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        [HttpGet]
        public async Task<IActionResult> GetStoklar(
            [FromQuery] int sayfa = 1,
            [FromQuery] int sayfaBoyutu = 20,
            [FromQuery] string? arama = null)
        {
            sayfaBoyutu = Math.Min(sayfaBoyutu, 1000);

            var sorgu = _context.Stoklar
                .Include(s => s.Birim)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(arama))
            {
                arama = arama.ToLower();
                sorgu = sorgu.Where(s =>
                    (s.StokKodu != null && s.StokKodu.ToLower().Contains(arama)) ||
                    (s.StokAdi != null && s.StokAdi.ToLower().Contains(arama)) ||
                    (s.YedekAdi != null && s.YedekAdi.ToLower().Contains(arama)) ||
                    (s.Birim.Ad != null && s.Birim.Ad.ToLower().Contains(arama)) ||
                    (s.Birim.Sembol != null && s.Birim.Sembol.ToLower().Contains(arama)));
            }

            var stoklar = await sorgu
                .OrderByDescending(s => s.KayitTarihi)
                .Skip((sayfa - 1) * sayfaBoyutu)
                .Take(sayfaBoyutu)
                .Select(s => new
                {
                    s.Id,
                    s.StokKodu,
                    s.StokAdi,
                    s.YedekAdi,
                    s.HksUrunId,
                    s.HksUretimSekliId,
                    s.HksUrunCinsiId,
                    s.HksNitelik,
                    s.BirimId,
                    s.KayitTarihi,
                    BirimAdi = s.Birim.Ad,
                    BirimSembolu = s.Birim.Sembol
                })
                .ToListAsync();

            var toplamKayit = await sorgu.CountAsync();

            return Ok(new
            {
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
            if (_currentUserService.MusteriId is null || _currentUserService.MusteriId == Guid.Empty)
            {
                return Unauthorized(new { hata = "Stok acabilmek icin bir sirket baglantisi gereklidir." });
            }

            var musteriId = _currentUserService.MusteriId.Value;
            var birimVarMi = await _context.Birimler
                .IgnoreQueryFilters()
                .AnyAsync(b => b.Id == dto.BirimId && b.MusteriId == musteriId && b.AktifMi);

            if (!birimVarMi)
            {
                return BadRequest(new { hata = "Secilen olcu birimi bulunamadi." });
            }

            var hksDogrulamaSonucu = await ValidateHksSelectionsAsync(dto, cancellationToken: HttpContext?.RequestAborted ?? CancellationToken.None);
            if (hksDogrulamaSonucu is not null)
            {
                return hksDogrulamaSonucu;
            }

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync<IActionResult>(async () =>
            {
                await using var transaction =
                    await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

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
                        HksUrunId = dto.HksUrunId,
                        HksUretimSekliId = dto.HksUretimSekliId,
                        HksUrunCinsiId = dto.HksUrunCinsiId,
                        HksNitelik = NormalizeNitelik(dto.HksNitelik),
                        BirimId = dto.BirimId
                    };

                    _context.Stoklar.Add(yeniStok);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Ok(new { mesaj = "Stok karti basariyla acildi.", stokKodu = yeniStok.StokKodu });
                }
                catch (DbUpdateException)
                {
                    await transaction.RollbackAsync();
                    return Conflict(new { hata = "Stok kodu uretiminde cakisma meydana geldi." });
                }
                catch
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, new { hata = "Stok kaydi olusturulamadi." });
                }
            });
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Guncelle(Guid id, [FromBody] StokDto dto)
        {
            var stok = await _context.Stoklar.FindAsync(id);
            if (stok is null)
            {
                return NotFound(new { hata = "Stok bulunamadi." });
            }

            if (!_currentUserService.IsSystemAdmin && stok.MusteriId != _currentUserService.MusteriId)
            {
                return Unauthorized(new { hata = "Bu stok uzerinde islem yapma yetkiniz yok." });
            }

            var birimVarMi = await _context.Birimler
                .IgnoreQueryFilters()
                .AnyAsync(b => b.Id == dto.BirimId && b.MusteriId == stok.MusteriId && b.AktifMi);
            if (!birimVarMi)
            {
                return BadRequest(new { hata = "Secilen olcu birimi bulunamadi." });
            }

            var hksDogrulamaSonucu = await ValidateHksSelectionsAsync(dto, cancellationToken: HttpContext?.RequestAborted ?? CancellationToken.None);
            if (hksDogrulamaSonucu is not null)
            {
                return hksDogrulamaSonucu;
            }

            stok.StokAdi = dto.StokAdi;
            stok.YedekAdi = dto.YedekAdi;
            stok.HksUrunId = dto.HksUrunId;
            stok.HksUretimSekliId = dto.HksUretimSekliId;
            stok.HksUrunCinsiId = dto.HksUrunCinsiId;
            stok.HksNitelik = NormalizeNitelik(dto.HksNitelik);
            stok.BirimId = dto.BirimId;

            await _context.SaveChangesAsync();
            return Ok(new { mesaj = "Stok karti guncellendi." });
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Sil(Guid id)
        {
            var query = _context.Stoklar
                .IgnoreQueryFilters()
                .Where(s => s.Id == id);

            if (!_currentUserService.IsSystemAdmin)
            {
                query = query.Where(s => s.MusteriId == _currentUserService.MusteriId);
            }

            var stok = await query.FirstOrDefaultAsync();

            if (stok is null)
            {
                return NotFound(new { hata = "Stok bulunamadi veya silme yetkiniz yok." });
            }

            _context.Stoklar.Remove(stok);
            await _context.SaveChangesAsync();

            return Ok(new { mesaj = "Stok karti silindi." });
        }

        private async Task<IActionResult?> ValidateHksSelectionsAsync(StokDto dto, CancellationToken cancellationToken)
        {
            var normalizedNitelik = NormalizeNitelik(dto.HksNitelik);
            if (normalizedNitelik is not null && !HksNitelikSecenekleri.Contains(normalizedNitelik, StringComparer.Ordinal))
            {
                return BadRequest(new { hata = "Secilen HKS niteligi gecersiz." });
            }

            if (dto.HksUrunId.HasValue)
            {
                var urunVarMi = await _context.HksUrunler
                    .IgnoreQueryFilters()
                    .AnyAsync(x => x.AktifMi && x.HksUrunId == dto.HksUrunId.Value, cancellationToken);

                if (!urunVarMi)
                {
                    return BadRequest(new { hata = "Secilen HKS urunu bulunamadi." });
                }
            }

            if (dto.HksUretimSekliId.HasValue)
            {
                var uretimSekliVarMi = await _context.HksUretimSekilleri
                    .IgnoreQueryFilters()
                    .AnyAsync(x => x.AktifMi && x.HksUretimSekliId == dto.HksUretimSekliId.Value, cancellationToken);

                if (!uretimSekliVarMi)
                {
                    return BadRequest(new { hata = "Secilen HKS uretim sekli bulunamadi." });
                }
            }

            if (!dto.HksUrunCinsiId.HasValue)
            {
                return null;
            }

            var urunCinsi = await _context.HksUrunCinsleri
                .IgnoreQueryFilters()
                .Where(x => x.AktifMi && x.HksUrunCinsiId == dto.HksUrunCinsiId.Value)
                .Select(x => new
                {
                    x.HksUrunId,
                    x.HksUretimSekliId,
                    x.IthalMi
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (urunCinsi is null)
            {
                return BadRequest(new { hata = "Secilen HKS urun cinsi bulunamadi." });
            }

            if (dto.HksUrunId.HasValue && dto.HksUrunId.Value != urunCinsi.HksUrunId)
            {
                return BadRequest(new { hata = "Secilen HKS urun cinsi urun secimiyle eslesmiyor." });
            }

            if (dto.HksUretimSekliId.HasValue && dto.HksUretimSekliId.Value != urunCinsi.HksUretimSekliId)
            {
                return BadRequest(new { hata = "Secilen HKS urun cinsi uretim sekliyle eslesmiyor." });
            }

            if (normalizedNitelik is null || urunCinsi.IthalMi is null)
            {
                return null;
            }

            var expectedNitelik = urunCinsi.IthalMi.Value ? "İthal" : "Yerli";
            if (!string.Equals(normalizedNitelik, expectedNitelik, StringComparison.Ordinal))
            {
                return BadRequest(new { hata = "Secilen HKS urun cinsi nitelik secimiyle eslesmiyor." });
            }

            return null;
        }

        private static string? NormalizeNitelik(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
