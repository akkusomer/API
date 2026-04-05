using AtlasWeb.Data;
using AtlasWeb.DTOs;
using AtlasWeb.Models;
using AtlasWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

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
            _context = context;
            _currentUserService = currentUserService;
        }

        [HttpGet]
        public async Task<IActionResult> GetCariKartlar(
            [FromQuery] int sayfa = 1,
            [FromQuery] int sayfaBoyutu = 20,
            [FromQuery] string? arama = null)
        {
            sayfaBoyutu = Math.Min(sayfaBoyutu, 1000);
            var currentTenantId = _currentUserService.MusteriId;
            var isSystemAdmin = _currentUserService.IsSystemAdmin;

            var sorgu = _context.CariKartlar
                .Include(ck => ck.CariTip)
                .Where(ck => ck.AktifMi)
                .AsQueryable();

            if (!isSystemAdmin)
            {
                if (!currentTenantId.HasValue)
                {
                    return Unauthorized(new { hata = "Cari kartlara erismek icin once bir sirkete bagli olmaniz gerekmektedir." });
                }

                sorgu = sorgu.Where(ck => ck.MusteriId == currentTenantId.Value);
            }

            if (!string.IsNullOrWhiteSpace(arama))
            {
                arama = arama.ToLower();
                sorgu = sorgu.Where(ck =>
                    (ck.Unvan != null && ck.Unvan.ToLower().Contains(arama)) ||
                    (ck.AdiSoyadi != null && ck.AdiSoyadi.ToLower().Contains(arama)) ||
                    (ck.VTCK_No != null && ck.VTCK_No.Contains(arama)) ||
                    (ck.Telefon != null && ck.Telefon.Contains(arama)) ||
                    (ck.Gsm != null && ck.Gsm.Contains(arama)) ||
                    (ck.HalIciIsyeriAdi != null && ck.HalIciIsyeriAdi.ToLower().Contains(arama)) ||
                    (ck.Il != null && ck.Il.ToLower().Contains(arama)) ||
                    (ck.Ilce != null && ck.Ilce.ToLower().Contains(arama)) ||
                    (ck.Belde != null && ck.Belde.ToLower().Contains(arama)) ||
                    _context.HksSifatlar.IgnoreQueryFilters().Any(x => x.AktifMi && ck.HksSifatId != null && x.HksSifatId == ck.HksSifatId && x.Ad.ToLower().Contains(arama)) ||
                    _context.HksIsletmeTurleri.IgnoreQueryFilters().Any(x => x.AktifMi && ck.HksIsletmeTuruId != null && x.HksIsletmeTuruId == ck.HksIsletmeTuruId && x.Ad.ToLower().Contains(arama)) ||
                    _context.HksIller.IgnoreQueryFilters().Any(x => x.AktifMi && ck.HksIlId != null && x.HksIlId == ck.HksIlId && x.Ad.ToLower().Contains(arama)) ||
                    _context.HksIlceler.IgnoreQueryFilters().Any(x => x.AktifMi && ck.HksIlceId != null && x.HksIlceId == ck.HksIlceId && x.Ad.ToLower().Contains(arama)) ||
                    _context.HksBeldeler.IgnoreQueryFilters().Any(x => x.AktifMi && ck.HksBeldeId != null && x.HksBeldeId == ck.HksBeldeId && x.Ad.ToLower().Contains(arama)));
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
                    ck.CariTipId,
                    ck.HksSifatId,
                    ck.HksHalIciIsyeriId,
                    ck.HksIsletmeTuruId,
                    ck.HksIlId,
                    ck.HksIlceId,
                    ck.HksBeldeId,
                    CariTipAdi = ck.CariTip != null ? ck.CariTip.Adi : null,
                    ck.Unvan,
                    ck.AdiSoyadi,
                    ck.FaturaTipi,
                    ck.GrupKodu,
                    ck.OzelKodu,
                    ck.Telefon,
                    ck.Telefon2,
                    ck.Gsm,
                    ck.Adres,
                    ck.HalIciIsyeriAdi,
                    Sifat = _context.HksSifatlar.IgnoreQueryFilters()
                        .Where(x => x.AktifMi && ck.HksSifatId != null && x.HksSifatId == ck.HksSifatId)
                        .Select(x => x.Ad)
                        .FirstOrDefault(),
                    IsletmeTuru = _context.HksIsletmeTurleri.IgnoreQueryFilters()
                        .Where(x => x.AktifMi && ck.HksIsletmeTuruId != null && x.HksIsletmeTuruId == ck.HksIsletmeTuruId)
                        .Select(x => x.Ad)
                        .FirstOrDefault(),
                    Il = _context.HksIller.IgnoreQueryFilters()
                        .Where(x => x.AktifMi && ck.HksIlId != null && x.HksIlId == ck.HksIlId)
                        .Select(x => x.Ad)
                        .FirstOrDefault() ?? ck.Il,
                    Ilce = _context.HksIlceler.IgnoreQueryFilters()
                        .Where(x => x.AktifMi && ck.HksIlceId != null && x.HksIlceId == ck.HksIlceId)
                        .Select(x => x.Ad)
                        .FirstOrDefault() ?? ck.Ilce,
                    Belde = _context.HksBeldeler.IgnoreQueryFilters()
                        .Where(x => x.AktifMi && ck.HksBeldeId != null && x.HksBeldeId == ck.HksBeldeId)
                        .Select(x => x.Ad)
                        .FirstOrDefault() ?? ck.Belde,
                    ck.VergiDairesi,
                    VtckNo = ck.VTCK_No,
                    DogumTarihi = ck.DogumTarihi,
                    ck.KayitTarihi
                })
                .ToListAsync();

            return Ok(new
            {
                veriler = kartlar,
                toplamKayit,
                mevcutSayfa = sayfa,
                sayfaBoyutu,
                toplamSayfa = (int)Math.Ceiling(toplamKayit / (double)sayfaBoyutu)
            });
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var kart = await _context.CariKartlar
                .IgnoreQueryFilters()
                .Include(ck => ck.CariTip)
                .FirstOrDefaultAsync(ck => ck.Id == id);

            if (kart == null)
            {
                return NotFound(new { hata = "Cari kart bulunamadi." });
            }

            if (!_currentUserService.IsSystemAdmin && kart.MusteriId != _currentUserService.MusteriId)
            {
                return Forbid();
            }

            return Ok(new
            {
                kart.Id,
                kart.MusteriId,
                kart.CariTipId,
                kart.HksSifatId,
                kart.HksHalIciIsyeriId,
                kart.HksIsletmeTuruId,
                kart.HksIlId,
                kart.HksIlceId,
                kart.HksBeldeId,
                CariTipAdi = kart.CariTip != null ? kart.CariTip.Adi : null,
                kart.Unvan,
                kart.AdiSoyadi,
                kart.FaturaTipi,
                kart.GrupKodu,
                kart.OzelKodu,
                kart.Telefon,
                kart.Telefon2,
                kart.Gsm,
                kart.Adres,
                kart.HalIciIsyeriAdi,
                Sifat = await ResolveSifatAdiAsync(kart.HksSifatId),
                IsletmeTuru = await ResolveIsletmeTuruAdiAsync(kart.HksIsletmeTuruId),
                Il = await ResolveIlAdiAsync(kart.HksIlId, kart.Il),
                Ilce = await ResolveIlceAdiAsync(kart.HksIlceId, kart.Ilce),
                Belde = await ResolveBeldeAdiAsync(kart.HksBeldeId, kart.Belde),
                kart.VergiDairesi,
                VtckNo = kart.VTCK_No,
                DogumTarihi = kart.DogumTarihi,
                kart.AktifMi,
                kart.KayitTarihi
            });
        }

        [HttpGet("{id:guid}/ekstre")]
        public async Task<IActionResult> GetEkstre(Guid id)
        {
            var kart = await _context.CariKartlar
                .AsNoTracking()
                .Include(ck => ck.CariTip)
                .FirstOrDefaultAsync(ck => ck.Id == id);

            if (kart == null)
            {
                return NotFound(new { hata = "Cari kart bulunamadi." });
            }

            if (!_currentUserService.IsSystemAdmin && kart.MusteriId != _currentUserService.MusteriId)
            {
                return Forbid();
            }

            var faturalarRaw = await _context.Faturalar
                .AsNoTracking()
                .Where(f => f.CariKartId == id)
                .OrderByDescending(f => f.FaturaTarihi)
                .ThenByDescending(f => f.KayitTarihi)
                .Select(f => new
                {
                    f.Id,
                    f.FaturaNo,
                    f.FaturaTarihi,
                    f.Aciklama,
                    f.Tutar,
                    f.TahsilEdilenTutar,
                    KalemSayisi = f.Detaylar.Count
                })
                .ToListAsync();

            var faturalar = faturalarRaw
                .Select(f => new
                {
                    f.Id,
                    f.FaturaNo,
                    f.FaturaTarihi,
                    f.Aciklama,
                    f.Tutar,
                    f.TahsilEdilenTutar,
                    KalanBorc = Math.Max(0, f.Tutar - f.TahsilEdilenTutar),
                    f.KalemSayisi
                })
                .ToList();

            var kasaFisleri = await _context.KasaFisleri
                .AsNoTracking()
                .Where(kf => kf.CariKartId == id)
                .OrderByDescending(kf => kf.Tarih)
                .ThenByDescending(kf => kf.KayitTarihi)
                .Select(kf => new
                {
                    kf.Id,
                    kf.BelgeNo,
                    kf.BelgeKodu,
                    kf.Tarih,
                    kf.IslemTipi,
                    kf.Tutar,
                    kf.Aciklama1
                })
                .ToListAsync();

            var toplamFaturaBorcu = faturalar.Sum(f => f.Tutar);
            var toplamFaturaTahsilat = faturalar.Sum(f => f.TahsilEdilenTutar);
            var toplamKasaTahsilat = kasaFisleri
                .Where(kf => kf.IslemTipi == KasaIslemTipi.Tahsilat)
                .Sum(kf => kf.Tutar);
            var toplamKasaOdeme = kasaFisleri
                .Where(kf => kf.IslemTipi == KasaIslemTipi.Odeme)
                .Sum(kf => kf.Tutar);
            var toplamBorc = toplamFaturaBorcu + toplamKasaOdeme;
            var toplamTahsilat = toplamFaturaTahsilat + toplamKasaTahsilat;

            return Ok(new
            {
                cari = new
                {
                    kart.Id,
                    kart.CariTipId,
                    kart.HksSifatId,
                    kart.HksHalIciIsyeriId,
                    kart.HksIsletmeTuruId,
                    kart.HksIlId,
                    kart.HksIlceId,
                    kart.HksBeldeId,
                    CariTipAdi = kart.CariTip != null ? kart.CariTip.Adi : null,
                    kart.Unvan,
                    kart.AdiSoyadi,
                    kart.Telefon,
                    kart.Gsm,
                    kart.VTCK_No,
                    VtckNo = kart.VTCK_No,
                    Sifat = await ResolveSifatAdiAsync(kart.HksSifatId),
                    kart.HalIciIsyeriAdi,
                    IsletmeTuru = await ResolveIsletmeTuruAdiAsync(kart.HksIsletmeTuruId),
                    Il = await ResolveIlAdiAsync(kart.HksIlId, kart.Il),
                    Ilce = await ResolveIlceAdiAsync(kart.HksIlceId, kart.Ilce),
                    Belde = await ResolveBeldeAdiAsync(kart.HksBeldeId, kart.Belde),
                    kart.DogumTarihi,
                    kart.Adres
                },
                ozet = new
                {
                    FaturaSayisi = faturalar.Count,
                    KasaFisSayisi = kasaFisleri.Count,
                    ToplamTutar = toplamFaturaBorcu,
                    ToplamKasaTahsilat = toplamKasaTahsilat,
                    ToplamKasaOdeme = toplamKasaOdeme,
                    ToplamBorc = toplamBorc,
                    ToplamTahsilat = toplamTahsilat,
                    KalanBorc = Math.Max(0, toplamBorc - toplamTahsilat),
                    SonFaturaTarihi = faturalar.FirstOrDefault()?.FaturaTarihi
                },
                faturalar,
                kasaFisleri
            });
        }

        [HttpPost]
        public async Task<IActionResult> Ekle([FromBody] CariKartDto dto)
        {
            if (_currentUserService.MusteriId == null || _currentUserService.MusteriId == Guid.Empty)
            {
                return Unauthorized(new { hata = "Cari kart acabilmek icin bir sirkete bagli olmalisiniz." });
            }

            var normalizedVtckNo = NormalizeOptional(dto.VTCK_No);

            var cariTipVarMi = await _context.CariTipler
                .IgnoreQueryFilters()
                .AnyAsync(ct => ct.Id == dto.CariTipId && ct.MusteriId == _currentUserService.MusteriId && ct.AktifMi);
            if (!cariTipVarMi)
            {
                return BadRequest(new { hata = "Secilen cari tip bulunamadi." });
            }

            if (normalizedVtckNo is not null)
            {
                var vtckCakisma = await _context.CariKartlar
                    .IgnoreQueryFilters()
                    .AnyAsync(ck => ck.MusteriId == _currentUserService.MusteriId && ck.VTCK_No == normalizedVtckNo && ck.AktifMi);

                if (vtckCakisma)
                {
                    return Conflict(new { hata = "Bu VKN/TCKN numarasina ait bir cari kart zaten kayitli." });
                }
            }

            if (dto.HksSifatId.HasValue && normalizedVtckNo is null)
            {
                return BadRequest(new { hata = "HKS sifat secmek icin VKN veya TCKN girilmelidir." });
            }

            if (dto.HksHalIciIsyeriId.HasValue && normalizedVtckNo is null)
            {
                return BadRequest(new { hata = "Hal ici isyeri secmek icin VKN veya TCKN girilmelidir." });
            }

            var konum = await ResolveLocationSelectionAsync(dto);
            if (konum.Hata is not null)
            {
                return BadRequest(new { hata = konum.Hata });
            }

            var sifat = await ResolveSifatSelectionAsync(dto);
            if (sifat.Hata is not null)
            {
                return BadRequest(new { hata = sifat.Hata });
            }

            var isletmeTuru = await ResolveIsletmeTuruSelectionAsync(dto);
            if (isletmeTuru.Hata is not null)
            {
                return BadRequest(new { hata = isletmeTuru.Hata });
            }

            var halIciIsyeri = ResolveHalIciIsyeriSelection(dto);

            var yeniKart = new CariKart
            {
                MusteriId = _currentUserService.MusteriId.Value,
                CariTipId = dto.CariTipId,
                Unvan = NormalizeOptional(dto.Unvan),
                AdiSoyadi = NormalizeOptional(dto.AdiSoyadi),
                FaturaTipi = dto.FaturaTipi,
                GrupKodu = NormalizeOptional(dto.GrupKodu),
                OzelKodu = NormalizeOptional(dto.OzelKodu),
                Telefon = NormalizeOptional(dto.Telefon),
                Telefon2 = NormalizeOptional(dto.Telefon2),
                Gsm = NormalizeOptional(dto.Gsm),
                Adres = NormalizeOptional(dto.Adres),
                HksSifatId = sifat.HksSifatId,
                HksHalIciIsyeriId = halIciIsyeri.HksHalIciIsyeriId,
                HalIciIsyeriAdi = halIciIsyeri.HalIciIsyeriAdi,
                HksIsletmeTuruId = isletmeTuru.HksIsletmeTuruId,
                HksIlId = konum.HksIlId,
                HksIlceId = konum.HksIlceId,
                HksBeldeId = konum.HksBeldeId,
                Il = konum.IlAdi,
                Ilce = konum.IlceAdi,
                Belde = konum.BeldeAdi,
                VergiDairesi = NormalizeOptional(dto.VergiDairesi),
                VTCK_No = normalizedVtckNo,
                DogumTarihi = NormalizeDateOnly(dto.DogumTarihi)
            };

            _context.CariKartlar.Add(yeniKart);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (IsCariVtckUniqueViolation(ex))
            {
                return Conflict(new { hata = "Bu VKN/TCKN numarasina ait bir cari kart zaten kayitli." });
            }

            return Ok(new { mesaj = "Cari kart basariyla olusturuldu.", id = yeniKart.Id });
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Guncelle(Guid id, [FromBody] CariKartDto dto)
        {
            var kart = await _context.CariKartlar.FindAsync(id);
            if (kart == null)
            {
                return NotFound(new { hata = "Cari kart bulunamadi." });
            }

            var normalizedVtckNo = NormalizeOptional(dto.VTCK_No);

            if (!_currentUserService.IsSystemAdmin && kart.MusteriId != _currentUserService.MusteriId)
            {
                return Forbid();
            }

            var cariTipVarMi = await _context.CariTipler
                .IgnoreQueryFilters()
                .AnyAsync(ct => ct.Id == dto.CariTipId && ct.MusteriId == kart.MusteriId && ct.AktifMi);
            if (!cariTipVarMi)
            {
                return BadRequest(new { hata = "Secilen cari tip bulunamadi." });
            }

            if (normalizedVtckNo is not null)
            {
                var vtckCakisma = await _context.CariKartlar
                    .IgnoreQueryFilters()
                    .AnyAsync(ck => ck.MusteriId == kart.MusteriId && ck.VTCK_No == normalizedVtckNo && ck.Id != id && ck.AktifMi);

                if (vtckCakisma)
                {
                    return Conflict(new { hata = "Bu VKN/TCKN numarasina ait baska bir cari kart zaten kayitli." });
                }
            }

            if (dto.HksSifatId.HasValue && normalizedVtckNo is null)
            {
                return BadRequest(new { hata = "HKS sifat secmek icin VKN veya TCKN girilmelidir." });
            }

            if (dto.HksHalIciIsyeriId.HasValue && normalizedVtckNo is null)
            {
                return BadRequest(new { hata = "Hal ici isyeri secmek icin VKN veya TCKN girilmelidir." });
            }

            var konum = await ResolveLocationSelectionAsync(dto);
            if (konum.Hata is not null)
            {
                return BadRequest(new { hata = konum.Hata });
            }

            var sifat = await ResolveSifatSelectionAsync(dto);
            if (sifat.Hata is not null)
            {
                return BadRequest(new { hata = sifat.Hata });
            }

            var isletmeTuru = await ResolveIsletmeTuruSelectionAsync(dto);
            if (isletmeTuru.Hata is not null)
            {
                return BadRequest(new { hata = isletmeTuru.Hata });
            }

            var halIciIsyeri = ResolveHalIciIsyeriSelection(dto);

            kart.CariTipId = dto.CariTipId;
            kart.Unvan = NormalizeOptional(dto.Unvan);
            kart.AdiSoyadi = NormalizeOptional(dto.AdiSoyadi);
            kart.FaturaTipi = dto.FaturaTipi;
            kart.GrupKodu = NormalizeOptional(dto.GrupKodu);
            kart.OzelKodu = NormalizeOptional(dto.OzelKodu);
            kart.Telefon = NormalizeOptional(dto.Telefon);
            kart.Telefon2 = NormalizeOptional(dto.Telefon2);
            kart.Gsm = NormalizeOptional(dto.Gsm);
            kart.Adres = NormalizeOptional(dto.Adres);
            kart.HksSifatId = sifat.HksSifatId;
            kart.HksHalIciIsyeriId = halIciIsyeri.HksHalIciIsyeriId;
            kart.HalIciIsyeriAdi = halIciIsyeri.HalIciIsyeriAdi;
            kart.HksIsletmeTuruId = isletmeTuru.HksIsletmeTuruId;
            kart.HksIlId = konum.HksIlId;
            kart.HksIlceId = konum.HksIlceId;
            kart.HksBeldeId = konum.HksBeldeId;
            kart.Il = konum.IlAdi;
            kart.Ilce = konum.IlceAdi;
            kart.Belde = konum.BeldeAdi;
            kart.VergiDairesi = NormalizeOptional(dto.VergiDairesi);
            kart.VTCK_No = normalizedVtckNo;
            kart.DogumTarihi = NormalizeDateOnly(dto.DogumTarihi);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (IsCariVtckUniqueViolation(ex))
            {
                return Conflict(new { hata = "Bu VKN/TCKN numarasina ait baska bir cari kart zaten kayitli." });
            }

            return Ok(new { mesaj = "Cari kart guncellendi." });
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Sil(Guid id)
        {
            var kart = await _context.CariKartlar
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (kart == null)
            {
                return NotFound(new { hata = "Cari kart bulunamadi." });
            }

            if (!_currentUserService.IsSystemAdmin && kart.MusteriId != _currentUserService.MusteriId)
            {
                return Forbid();
            }

            if (!kart.AktifMi)
            {
                return Ok(new { mesaj = "Cari kart zaten pasif durumda." });
            }

            _context.CariKartlar.Remove(kart);
            await _context.SaveChangesAsync();

            return Ok(new { mesaj = "Cari kart pasife alindi." });
        }

        private async Task<SifatSelectionResult> ResolveSifatSelectionAsync(CariKartDto dto)
        {
            if (!dto.HksSifatId.HasValue)
            {
                return new SifatSelectionResult(null, null);
            }

            var sifat = await _context.HksSifatlar
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.AktifMi && x.HksSifatId == dto.HksSifatId.Value);

            if (sifat is null)
            {
                return new SifatSelectionResult(null, "Secilen HKS sifat kaydi bulunamadi.");
            }

            return new SifatSelectionResult(sifat.HksSifatId, null);
        }

        private async Task<IsletmeTuruSelectionResult> ResolveIsletmeTuruSelectionAsync(CariKartDto dto)
        {
            if (!dto.HksIsletmeTuruId.HasValue)
            {
                return new IsletmeTuruSelectionResult(null, null);
            }

            var isletmeTuru = await _context.HksIsletmeTurleri
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.AktifMi && x.HksIsletmeTuruId == dto.HksIsletmeTuruId.Value);

            if (isletmeTuru is null)
            {
                return new IsletmeTuruSelectionResult(null, "Secilen HKS isletme turu kaydi bulunamadi.");
            }

            return new IsletmeTuruSelectionResult(isletmeTuru.HksIsletmeTuruId, null);
        }

        private static HalIciIsyeriSelectionResult ResolveHalIciIsyeriSelection(CariKartDto dto)
        {
            if (!dto.HksHalIciIsyeriId.HasValue)
            {
                return new HalIciIsyeriSelectionResult(null, null);
            }

            return new HalIciIsyeriSelectionResult(
                dto.HksHalIciIsyeriId.Value,
                NormalizeOptional(dto.HalIciIsyeriAdi));
        }

        private async Task<LocationSelectionResult> ResolveLocationSelectionAsync(CariKartDto dto)
        {
            var ilResult = await ResolveIlAsync(dto);
            if (ilResult.Hata is not null)
            {
                return ilResult;
            }

            var ilceResult = await ResolveIlceAsync(dto, ilResult.HksIlId);
            if (ilceResult.Hata is not null)
            {
                return ilceResult;
            }

            var beldeResult = await ResolveBeldeAsync(dto, ilceResult.HksIlceId);
            if (beldeResult.Hata is not null)
            {
                return beldeResult;
            }

            return new LocationSelectionResult(
                ilResult.HksIlId,
                ilceResult.HksIlceId,
                beldeResult.HksBeldeId,
                ilResult.IlAdi,
                ilceResult.IlceAdi,
                beldeResult.BeldeAdi,
                null);
        }

        private async Task<LocationSelectionResult> ResolveIlAsync(CariKartDto dto)
        {
            var ilId = dto.HksIlId;
            var ilAdi = NormalizeOptional(dto.Il);

            if (!ilId.HasValue && ilAdi is null)
            {
                return new LocationSelectionResult(null, null, null, null, null, null, null);
            }

            HksIl? il;
            if (ilId.HasValue)
            {
                il = await _context.HksIller
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(x => x.AktifMi && x.HksIlId == ilId.Value);
            }
            else
            {
                il = await _context.HksIller
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(x => x.AktifMi && x.Ad == ilAdi);
            }

            if (il is null)
            {
                return new LocationSelectionResult(null, null, null, null, null, null, "Secilen il bulunamadi.");
            }

            return new LocationSelectionResult(il.HksIlId, null, null, il.Ad, null, null, null);
        }

        private async Task<LocationSelectionResult> ResolveIlceAsync(CariKartDto dto, int? hksIlId)
        {
            var ilceId = dto.HksIlceId;
            var ilceAdi = NormalizeOptional(dto.Ilce);

            if (!ilceId.HasValue && ilceAdi is null)
            {
                return new LocationSelectionResult(null, null, null, null, null, null, null);
            }

            if (!hksIlId.HasValue)
            {
                return new LocationSelectionResult(null, null, null, null, null, null, "Ilce secimi icin once il secilmelidir.");
            }

            HksIlce? ilce;
            if (ilceId.HasValue)
            {
                ilce = await _context.HksIlceler
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(x => x.AktifMi && x.HksIlceId == ilceId.Value && x.HksIlId == hksIlId.Value);
            }
            else
            {
                ilce = await _context.HksIlceler
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(x => x.AktifMi && x.HksIlId == hksIlId.Value && x.Ad == ilceAdi);
            }

            if (ilce is null)
            {
                return new LocationSelectionResult(null, null, null, null, null, null, "Secilen ilce il secimiyle eslesmiyor.");
            }

            return new LocationSelectionResult(null, ilce.HksIlceId, null, null, ilce.Ad, null, null);
        }

        private async Task<LocationSelectionResult> ResolveBeldeAsync(CariKartDto dto, int? hksIlceId)
        {
            var beldeId = dto.HksBeldeId;
            var beldeAdi = NormalizeOptional(dto.Belde);

            if (!beldeId.HasValue && beldeAdi is null)
            {
                return new LocationSelectionResult(null, null, null, null, null, null, null);
            }

            if (!hksIlceId.HasValue)
            {
                return new LocationSelectionResult(null, null, null, null, null, null, "Belde secimi icin once ilce secilmelidir.");
            }

            HksBelde? belde;
            if (beldeId.HasValue)
            {
                belde = await _context.HksBeldeler
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(x => x.AktifMi && x.HksBeldeId == beldeId.Value && x.HksIlceId == hksIlceId.Value);
            }
            else
            {
                belde = await _context.HksBeldeler
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(x => x.AktifMi && x.HksIlceId == hksIlceId.Value && x.Ad == beldeAdi);
            }

            if (belde is null)
            {
                return new LocationSelectionResult(null, null, null, null, null, null, "Secilen belde ilce secimiyle eslesmiyor.");
            }

            return new LocationSelectionResult(null, null, belde.HksBeldeId, null, null, belde.Ad, null);
        }

        private async Task<string?> ResolveIlAdiAsync(int? hksIlId, string? fallback)
        {
            if (!hksIlId.HasValue)
            {
                return fallback;
            }

            return await _context.HksIller
                .IgnoreQueryFilters()
                .Where(x => x.AktifMi && x.HksIlId == hksIlId.Value)
                .Select(x => x.Ad)
                .FirstOrDefaultAsync() ?? fallback;
        }

        private async Task<string?> ResolveIlceAdiAsync(int? hksIlceId, string? fallback)
        {
            if (!hksIlceId.HasValue)
            {
                return fallback;
            }

            return await _context.HksIlceler
                .IgnoreQueryFilters()
                .Where(x => x.AktifMi && x.HksIlceId == hksIlceId.Value)
                .Select(x => x.Ad)
                .FirstOrDefaultAsync() ?? fallback;
        }

        private async Task<string?> ResolveBeldeAdiAsync(int? hksBeldeId, string? fallback)
        {
            if (!hksBeldeId.HasValue)
            {
                return fallback;
            }

            return await _context.HksBeldeler
                .IgnoreQueryFilters()
                .Where(x => x.AktifMi && x.HksBeldeId == hksBeldeId.Value)
                .Select(x => x.Ad)
                .FirstOrDefaultAsync() ?? fallback;
        }

        private async Task<string?> ResolveSifatAdiAsync(int? hksSifatId)
        {
            if (!hksSifatId.HasValue)
            {
                return null;
            }

            return await _context.HksSifatlar
                .IgnoreQueryFilters()
                .Where(x => x.AktifMi && x.HksSifatId == hksSifatId.Value)
                .Select(x => x.Ad)
                .FirstOrDefaultAsync();
        }

        private async Task<string?> ResolveIsletmeTuruAdiAsync(int? hksIsletmeTuruId)
        {
            if (!hksIsletmeTuruId.HasValue)
            {
                return null;
            }

            return await _context.HksIsletmeTurleri
                .IgnoreQueryFilters()
                .Where(x => x.AktifMi && x.HksIsletmeTuruId == hksIsletmeTuruId.Value)
                .Select(x => x.Ad)
                .FirstOrDefaultAsync();
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static DateTime? NormalizeDateOnly(DateTime? value)
        {
            if (!value.HasValue)
            {
                return null;
            }

            return DateTime.SpecifyKind(value.Value.Date, DateTimeKind.Unspecified);
        }

        private static bool IsCariVtckUniqueViolation(DbUpdateException ex)
        {
            return ex.InnerException is PostgresException pg && pg.ConstraintName == "IX_CariKartlar_MusteriId_VTCK_No_Unique";
        }

        private sealed record LocationSelectionResult(
            int? HksIlId,
            int? HksIlceId,
            int? HksBeldeId,
            string? IlAdi,
            string? IlceAdi,
            string? BeldeAdi,
            string? Hata);

        private sealed record SifatSelectionResult(
            int? HksSifatId,
            string? Hata);

        private sealed record IsletmeTuruSelectionResult(
            int? HksIsletmeTuruId,
            string? Hata);

        private sealed record HalIciIsyeriSelectionResult(
            int? HksHalIciIsyeriId,
            string? HalIciIsyeriAdi);
    }
}
