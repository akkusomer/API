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
    public class CariKartController : ControllerBase
    {
        private readonly AtlasDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public CariKartController(AtlasDbContext context, ICurrentUserService currentUserService)
        {
            _context            = context;
            _currentUserService = currentUserService;
        }

        /// <summary>
        /// Tenant bazlı sayfalı cari kart listesi.
        /// Admin tüm kiracıları, user yalnızca kendi kiracısını görür (HasQueryFilter).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCariKartlar(
            [FromQuery] int sayfa = 1,
            [FromQuery] int sayfaBoyutu = 20,
            [FromQuery] string? arama = null)
        {
            sayfaBoyutu = Math.Min(sayfaBoyutu, 100);

            var sorgu = _context.CariKartlar
                .Include(ck => ck.CariTip)
                .AsQueryable();

            // Arama filtresi (unvan, adsoyad veya vergi no)
            if (!string.IsNullOrWhiteSpace(arama))
            {
                arama = arama.ToLower();
                sorgu = sorgu.Where(ck =>
                    (ck.Unvan      != null && ck.Unvan.ToLower().Contains(arama)) ||
                    (ck.AdiSoyadi  != null && ck.AdiSoyadi.ToLower().Contains(arama)) ||
                    (ck.VTCK_No    != null && ck.VTCK_No.Contains(arama)) ||
                    (ck.Telefon    != null && ck.Telefon.Contains(arama)) ||
                    (ck.Gsm        != null && ck.Gsm.Contains(arama)));
            }

            var toplamKayit = await sorgu.CountAsync();

            var kartlar = await sorgu
                .OrderByDescending(ck => ck.KayitTarihi)
                .Skip((sayfa - 1) * sayfaBoyutu)
                .Take(sayfaBoyutu)
                .Select(ck => new
                {
                    ck.Id,
                    ck.MusteriId,
                    CariTipAdi    = ck.CariTip != null ? ck.CariTip.Adi : null,
                    ck.Unvan,
                    ck.AdiSoyadi,
                    ck.FaturaTipi,
                    ck.GrupKodu,
                    ck.OzelKodu,
                    ck.Telefon,
                    ck.Gsm,
                    ck.Adres,
                    ck.VergiDairesi,
                    ck.VTCK_No,
                    ck.KayitTarihi
                })
                .ToListAsync();

            return Ok(new
            {
                veriler      = kartlar,
                toplamKayit,
                mevcutSayfa  = sayfa,
                sayfaBoyutu,
                toplamSayfa  = (int)Math.Ceiling(toplamKayit / (double)sayfaBoyutu)
            });
        }

        /// <summary>Tek cari kartın tam detayı.</summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var kart = await _context.CariKartlar
                .Include(ck => ck.CariTip)
                .FirstOrDefaultAsync(ck => ck.Id == id);

            if (kart == null) return NotFound(new { hata = "Cari kart bulunamadı." });

            return Ok(kart);
        }

        /// <summary>Yeni cari kart oluşturur.</summary>
        [HttpPost]
        public async Task<IActionResult> Ekle([FromBody] CariKartDto dto)
        {
            if (_currentUserService.MusteriId == null || _currentUserService.MusteriId == Guid.Empty)
                return Unauthorized(new { hata = "Cari kart açabilmek için bir şirkete bağlı olmalısınız." });

            // CariTip mevcut mu?
            var cariTipVarMi = await _context.CariTipler.AnyAsync(ct => ct.Id == dto.CariTipId);
            if (!cariTipVarMi)
                return BadRequest(new { hata = "Seçilen cari tip bulunamadı." });

            // Aynı VTCK_No ile aynı kiracıda cari kart var mı?
            if (!string.IsNullOrWhiteSpace(dto.VTCK_No))
            {
                var vtckCakisma = await _context.CariKartlar
                    .IgnoreQueryFilters()
                    .AnyAsync(ck => ck.MusteriId == _currentUserService.MusteriId && ck.VTCK_No == dto.VTCK_No && ck.AktifMi);

                if (vtckCakisma)
                    return Conflict(new { hata = "Bu VKN/TCKN numarasına ait bir cari kart zaten kayıtlı." });
            }

            var yeniKart = new CariKart
            {
                MusteriId    = _currentUserService.MusteriId.Value,
                CariTipId    = dto.CariTipId,
                Unvan        = dto.Unvan,
                AdiSoyadi    = dto.AdiSoyadi,
                FaturaTipi   = dto.FaturaTipi,
                GrupKodu     = dto.GrupKodu,
                OzelKodu     = dto.OzelKodu,
                Telefon      = dto.Telefon,
                Telefon2     = dto.Telefon2,
                Gsm          = dto.Gsm,
                Adres        = dto.Adres,
                VergiDairesi = dto.VergiDairesi,
                VTCK_No      = dto.VTCK_No
            };

            _context.CariKartlar.Add(yeniKart);
            await _context.SaveChangesAsync();

            return Ok(new { mesaj = "Cari kart başarıyla oluşturuldu.", id = yeniKart.Id });
        }

        /// <summary>Mevcut cari kartı günceller.</summary>
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Guncelle(Guid id, [FromBody] CariKartDto dto)
        {
            var kart = await _context.CariKartlar.FindAsync(id);
            if (kart == null) return NotFound(new { hata = "Cari kart bulunamadı." });

            // Tenant güvenliği — admin olmayan kendi dışına erişemez
            if (!_currentUserService.IsAdmin && kart.MusteriId != _currentUserService.MusteriId)
                return Forbid();

            // CariTip mevcut mu?
            var cariTipVarMi = await _context.CariTipler.AnyAsync(ct => ct.Id == dto.CariTipId);
            if (!cariTipVarMi)
                return BadRequest(new { hata = "Seçilen cari tip bulunamadı." });

            // VKN/TCKN çakışma kontrolü (kendisi hariç)
            if (!string.IsNullOrWhiteSpace(dto.VTCK_No))
            {
                var vtckCakisma = await _context.CariKartlar
                    .IgnoreQueryFilters()
                    .AnyAsync(ck => ck.MusteriId == kart.MusteriId && ck.VTCK_No == dto.VTCK_No && ck.Id != id && ck.AktifMi);

                if (vtckCakisma)
                    return Conflict(new { hata = "Bu VKN/TCKN numarasına ait başka bir cari kart zaten kayıtlı." });
            }

            kart.CariTipId    = dto.CariTipId;
            kart.Unvan        = dto.Unvan;
            kart.AdiSoyadi    = dto.AdiSoyadi;
            kart.FaturaTipi   = dto.FaturaTipi;
            kart.GrupKodu     = dto.GrupKodu;
            kart.OzelKodu     = dto.OzelKodu;
            kart.Telefon      = dto.Telefon;
            kart.Telefon2     = dto.Telefon2;
            kart.Gsm          = dto.Gsm;
            kart.Adres        = dto.Adres;
            kart.VergiDairesi = dto.VergiDairesi;
            kart.VTCK_No      = dto.VTCK_No;

            await _context.SaveChangesAsync();
            return Ok(new { mesaj = "Cari kart güncellendi." });
        }

        /// <summary>Cari kartı soft-delete ile pasife çeker.</summary>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Sil(Guid id)
        {
            var kart = await _context.CariKartlar.FindAsync(id);
            if (kart == null) return NotFound(new { hata = "Cari kart bulunamadı." });

            if (!_currentUserService.IsAdmin && kart.MusteriId != _currentUserService.MusteriId)
                return Forbid();

            _context.CariKartlar.Remove(kart); // SaveChangesAsync → soft delete
            await _context.SaveChangesAsync();

            return Ok(new { mesaj = "Cari kart pasife alındı." });
        }
    }
}
