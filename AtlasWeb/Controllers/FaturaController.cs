using System.Globalization;
using System.Text.RegularExpressions;
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
    public class FaturaController : ControllerBase
    {
        private static readonly Regex InvoiceNumberRegex = new(@"(\d+)$", RegexOptions.Compiled);

        private readonly AtlasDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IHksService? _hksService;

        public FaturaController(AtlasDbContext context, ICurrentUserService currentUserService, IHksService? hksService = null)
        {
            _context = context;
            _currentUserService = currentUserService;
            _hksService = hksService;
        }

        [HttpGet]
        public async Task<IActionResult> GetFaturalar(
            [FromQuery] int sayfa = 1,
            [FromQuery] int sayfaBoyutu = 20,
            [FromQuery] string? arama = null)
        {
            sayfaBoyutu = Math.Min(sayfaBoyutu, 100);

            var sorgu = _context.Faturalar
                .Include(f => f.CariKart)
                .Include(f => f.Detaylar)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(arama))
            {
                var term = arama.Trim().ToLower();
                sorgu = sorgu.Where(f =>
                    f.FaturaNo.ToLower().Contains(term)
                    || f.Detaylar.Any(d => d.AlisKunye != null && d.AlisKunye.ToLower().Contains(term))
                    || f.Detaylar.Any(d => d.SatisKunye != null && d.SatisKunye.ToLower().Contains(term))
                    || (f.Aciklama != null && f.Aciklama.ToLower().Contains(term))
                    || (f.CariKart != null && (
                        (f.CariKart.Unvan != null && f.CariKart.Unvan.ToLower().Contains(term))
                        || (f.CariKart.AdiSoyadi != null && f.CariKart.AdiSoyadi.ToLower().Contains(term))
                    )));
            }

            var toplamKayit = await sorgu.CountAsync();

            var veriler = await sorgu
                .OrderByDescending(f => f.FaturaTarihi)
                .ThenByDescending(f => f.KayitTarihi)
                .Skip((sayfa - 1) * sayfaBoyutu)
                .Take(sayfaBoyutu)
                .Select(f => new
                {
                    f.Id,
                    f.FaturaNo,
                    f.CariKartId,
                    CariAdi = f.CariKart != null
                        ? (f.CariKart.Unvan ?? f.CariKart.AdiSoyadi)
                        : null,
                    f.FaturaTarihi,
                    AlisKunye = f.Detaylar
                        .Where(d => d.AlisKunye != null && d.AlisKunye != string.Empty)
                        .OrderBy(d => d.KayitTarihi)
                        .Select(d => d.AlisKunye)
                        .FirstOrDefault(),
                    f.Aciklama,
                    f.Tutar,
                    f.TahsilEdilenTutar,
                    KalemSayisi = f.Detaylar.Count
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

        [HttpGet("ozet")]
        public async Task<IActionResult> GetOzet()
        {
            var today = GetIstanbulToday();
            var monthStart = AsUtcDate(new DateTime(today.Year, today.Month, 1));
            var nextMonthStart = monthStart.AddMonths(1);

            var sorgu = _context.Faturalar
                .AsNoTracking()
                .Include(f => f.CariKart)
                .AsQueryable();

            var toplamFatura = await sorgu.CountAsync();
            var toplamTutar = await sorgu.Select(f => (decimal?)f.Tutar).SumAsync() ?? 0m;

            var bugunSorgu = sorgu.Where(f => f.FaturaTarihi.Date == today);
            var buAySorgu = sorgu.Where(f => f.FaturaTarihi.Date >= monthStart && f.FaturaTarihi.Date < nextMonthStart);

            var bugunFatura = await bugunSorgu.CountAsync();
            var bugunTutar = await bugunSorgu.Select(f => (decimal?)f.Tutar).SumAsync() ?? 0m;
            var buAyFatura = await buAySorgu.CountAsync();
            var buAyTutar = await buAySorgu.Select(f => (decimal?)f.Tutar).SumAsync() ?? 0m;

            var sonFaturalar = await sorgu
                .OrderByDescending(f => f.FaturaTarihi)
                .ThenByDescending(f => f.KayitTarihi)
                .Take(5)
                .Select(f => new
                {
                    f.Id,
                    f.FaturaNo,
                    f.FaturaTarihi,
                    f.Tutar,
                    CariAdi = f.CariKart != null
                        ? (f.CariKart.Unvan ?? f.CariKart.AdiSoyadi)
                        : null
                })
                .ToListAsync();

            return Ok(new
            {
                toplamFatura,
                toplamTutar,
                bugunFatura,
                bugunTutar,
                buAyFatura,
                buAyTutar,
                sonFaturalar
            });
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var fatura = await _context.Faturalar
                .Include(f => f.CariKart)
                .Include(f => f.Detaylar.OrderBy(d => d.KayitTarihi))
                .ThenInclude(d => d.Stok)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (fatura is null)
            {
                return NotFound(new { hata = "Fatura bulunamadi." });
            }

            return Ok(new
            {
                fatura.Id,
                fatura.FaturaNo,
                fatura.CariKartId,
                CariAdi = fatura.CariKart != null
                    ? (fatura.CariKart.Unvan ?? fatura.CariKart.AdiSoyadi)
                    : null,
                fatura.FaturaTarihi,
                fatura.Aciklama,
                fatura.Tutar,
                fatura.TahsilEdilenTutar,
                Kalemler = fatura.Detaylar
                    .OrderBy(d => d.KayitTarihi)
                    .Select(d => new
                    {
                        d.Id,
                        d.StokId,
                        StokKodu = d.Stok.StokKodu,
                        StokAdi = d.Stok.StokAdi,
                        d.AlisKunye,
                        d.SatisKunye,
                        d.Miktar,
                        d.BirimFiyat,
                        d.SatirToplami
                    })
                    .ToList()
            });
        }

        [HttpPost]
        public async Task<IActionResult> Ekle([FromBody] FaturaDto dto)
        {
            if (_currentUserService.MusteriId is null || _currentUserService.MusteriId == Guid.Empty)
            {
                return Unauthorized(new { hata = "Fatura acabilmek icin bir sirket baglantisi gereklidir." });
            }

            var musteriId = _currentUserService.MusteriId.Value;
            var validation = await ValidateTenantReferencesAsync(musteriId, dto);
            if (validation is not null)
            {
                return validation;
            }

            var invoiceTotal = CalculateInvoiceTotal(dto);
            var tahsilatValidation = ValidateTahsilatAmount(dto.TahsilEdilenTutar, invoiceTotal);
            if (tahsilatValidation is not null)
            {
                return tahsilatValidation;
            }

            var executionStrategy = _context.Database.CreateExecutionStrategy();

            return await executionStrategy.ExecuteAsync<IActionResult>(async () =>
            {
                const int maxCreateAttempts = 5;
                for (var attempt = 1; attempt <= maxCreateAttempts; attempt++)
                {
                    await using var transaction = await _context.Database.BeginTransactionAsync();

                    try
                    {
                        if (IsPostgresProvider())
                        {
                            var tenantLockKey = GetTenantInvoiceLockKey(musteriId);
                            await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({tenantLockKey})");
                        }

                        var fallbackInvoiceNumber = attempt == maxCreateAttempts
                            ? GenerateFallbackInvoiceNumber()
                            : null;

                        var fatura = await BuildInvoiceAsync(musteriId, dto, invoiceTotal, fallbackInvoiceNumber);
                        _context.Faturalar.Add(fatura);
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        return Ok(new
                        {
                            mesaj = "Fatura basariyla olusturuldu.",
                            id = fatura.Id,
                            faturaNo = fatura.FaturaNo
                        });
                    }
                    catch (DbUpdateException ex) when (IsInvoiceNumberConflict(ex) && attempt < maxCreateAttempts)
                    {
                        await transaction.RollbackAsync();
                        _context.ChangeTracker.Clear();
                    }
                    catch (DbUpdateException ex)
                    {
                        await transaction.RollbackAsync();
                        _context.ChangeTracker.Clear();
                        return BuildInvoiceCreateDbErrorResult(ex);
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        _context.ChangeTracker.Clear();
                        return StatusCode(500, new { hata = "Fatura olusturulamadi." });
                    }
                }

                return Conflict(new { hata = "Fatura numarasi uretiminde cakisma olustu." });
            });
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Guncelle(Guid id, [FromBody] FaturaDto dto)
        {
            var fatura = await _context.Faturalar
                .Include(f => f.Detaylar)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (fatura is null)
            {
                return NotFound(new { hata = "Fatura bulunamadi." });
            }

            if (!_currentUserService.IsSystemAdmin && fatura.MusteriId != _currentUserService.MusteriId)
            {
                return Forbid();
            }

            var validation = await ValidateTenantReferencesAsync(fatura.MusteriId, dto);
            if (validation is not null)
            {
                return validation;
            }

            var invoiceTotal = CalculateInvoiceTotal(dto);
            var tahsilatValidation = ValidateTahsilatAmount(dto.TahsilEdilenTutar, invoiceTotal);
            if (tahsilatValidation is not null)
            {
                return tahsilatValidation;
            }

            fatura.CariKartId = dto.CariKartId;
            fatura.FaturaTarihi = NormalizeInvoiceDate(dto.FaturaTarihi);
            fatura.AlisKunye = null;
            fatura.Aciklama = string.IsNullOrWhiteSpace(dto.Aciklama) ? null : dto.Aciklama.Trim();
            fatura.Tutar = invoiceTotal;
            fatura.TahsilEdilenTutar = NormalizeTahsilatAmount(dto.TahsilEdilenTutar);

            _context.FaturaDetaylari.RemoveRange(fatura.Detaylar);
            _context.FaturaDetaylari.AddRange(dto.Kalemler.Select(k => new FaturaDetay
            {
                MusteriId = fatura.MusteriId,
                FaturaId = fatura.Id,
                StokId = k.StokId,
                AlisKunye = NormalizeAlisKunye(k.AlisKunye),
                SatisKunye = NormalizeAlisKunye(k.SatisKunye),
                Miktar = k.Miktar,
                BirimFiyat = k.BirimFiyat,
                SatirToplami = Math.Round(k.Miktar * k.BirimFiyat, 2, MidpointRounding.AwayFromZero)
            }));

            await _context.SaveChangesAsync();
            return Ok(new { mesaj = "Fatura guncellendi." });
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Sil(Guid id)
        {
            var fatura = await _context.Faturalar
                .Include(f => f.Detaylar)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (fatura is null)
            {
                return NotFound(new { hata = "Fatura bulunamadi." });
            }

            if (!_currentUserService.IsSystemAdmin && fatura.MusteriId != _currentUserService.MusteriId)
            {
                return Forbid();
            }

            _context.FaturaDetaylari.RemoveRange(fatura.Detaylar);
            _context.Faturalar.Remove(fatura);
            await _context.SaveChangesAsync();

            return Ok(new { mesaj = "Fatura pasife alindi." });
        }

        [HttpPost("{id:guid}/satis-kunye")]
        public async Task<IActionResult> SatisKunyeOlustur(Guid id, [FromBody] FaturaSatisKunyeTalepDto dto, CancellationToken cancellationToken)
        {
            if (_hksService is null)
            {
                return StatusCode(501, new { hata = "HKS satis künye servisi hazir degil." });
            }

            var fatura = await _context.Faturalar
                .Include(f => f.CariKart)
                .Include(f => f.Detaylar.OrderBy(d => d.KayitTarihi))
                .ThenInclude(d => d.Stok)
                .ThenInclude(s => s.Birim)
                .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

            if (fatura is null)
            {
                return NotFound(new { hata = "Fatura bulunamadi." });
            }

            if (!_currentUserService.IsSystemAdmin && fatura.MusteriId != _currentUserService.MusteriId)
            {
                return Forbid();
            }

            if (dto.BildirimciSifatId <= 0)
            {
                return BadRequest(new { hata = "Bildirimci sifat secilmelidir." });
            }

            if (dto.BildirimTuruId <= 0)
            {
                return BadRequest(new { hata = "Bildirim turu secilmelidir." });
            }

            if (dto.BelgeTipiId <= 0)
            {
                return BadRequest(new { hata = "Belge tipi secilmelidir." });
            }

            var belgeNo = string.IsNullOrWhiteSpace(dto.BelgeNo) ? fatura.FaturaNo : dto.BelgeNo.Trim();
            if (string.IsNullOrWhiteSpace(belgeNo))
            {
                return BadRequest(new { hata = "Belge no bos olamaz." });
            }

            if (fatura.CariKart is null)
            {
                return BadRequest(new { hata = "Faturaya bagli cari kart bulunamadi." });
            }

            if (string.IsNullOrWhiteSpace(fatura.CariKart.VTCK_No))
            {
                return BadRequest(new { hata = "Cari kartta VKN / TCKN bilgisi olmadan satis künyesi olusturulamaz." });
            }

            if (!fatura.CariKart.HksSifatId.HasValue || fatura.CariKart.HksSifatId.Value <= 0)
            {
                return BadRequest(new { hata = "Cari kartta HKS sifat bilgisi secilmelidir." });
            }

            var buyerIdentity = NormalizeBuyerIdentity(fatura.CariKart.VTCK_No);
            if (buyerIdentity is null)
            {
                return BadRequest(new { hata = "Cari karttaki VKN / TCKN gecersiz. 10 veya 11 haneli sayisal deger girilmelidir." });
            }

            var buyerDisplayName = ResolveCariDisplayName(fatura.CariKart);
            if (string.IsNullOrWhiteSpace(buyerDisplayName))
            {
                return BadRequest(new { hata = "Cari kartta unvan veya ad soyad bilgisi olmadan satis kunyesi olusturulamaz." });
            }

            var rawBuyerPhone = $"{fatura.CariKart.Gsm} {fatura.CariKart.Telefon}".Trim();
            var buyerPhone = NormalizeHksMobilePhone(fatura.CariKart.Gsm)
                ?? NormalizeHksMobilePhone(fatura.CariKart.Telefon);
            if (string.IsNullOrWhiteSpace(rawBuyerPhone))
            {
                return BadRequest(new { hata = "Cari kartta GSM veya telefon bilgisi olmadan satis kunyesi olusturulamaz." });
            }

            if (string.IsNullOrWhiteSpace(buyerPhone))
            {
                return BadRequest(new
                {
                    hata = "Cari karttaki GSM bilgisi HKS icin gecersiz. 10 haneli cep telefonu girilmelidir."
                });
            }

            if (!fatura.CariKart.HksIsletmeTuruId.HasValue || fatura.CariKart.HksIsletmeTuruId.Value <= 0)
            {
                return BadRequest(new { hata = "Cari kartta HKS isletme turu secilmelidir." });
            }

            if (!fatura.CariKart.HksIlId.HasValue || fatura.CariKart.HksIlId.Value <= 0
                || !fatura.CariKart.HksIlceId.HasValue || fatura.CariKart.HksIlceId.Value <= 0
                || !fatura.CariKart.HksBeldeId.HasValue || fatura.CariKart.HksBeldeId.Value <= 0)
            {
                return BadRequest(new { hata = "Cari kartta HKS il / ilce / belde bilgileri eksik." });
            }

            var hedefDetaylar = fatura.Detaylar
                .Where(d => string.IsNullOrWhiteSpace(d.SatisKunye))
                .ToList();

            if (hedefDetaylar.Count == 0)
            {
                return BadRequest(new { hata = "Tum kalemlerde satis künye zaten mevcut." });
            }

            try
            {
                var hksBirimleri = await _hksService.GetUrunBirimleriAsync(cancellationToken);
                var malinNitelikleri = await _hksService.GetMalinNitelikleriAsync(cancellationToken);
                var isletmeTurleri = await _hksService.GetIsletmeTurleriAsync(cancellationToken);
                var seciliIsletmeTuru = isletmeTurleri.FirstOrDefault(item => item.Id == fatura.CariKart.HksIsletmeTuruId.Value);
                if (seciliIsletmeTuru is null)
                {
                    return BadRequest(new { hata = "Cari kartta secili HKS isletme turu gecersiz." });
                }

                var kayitliKisi = await _hksService.GetKayitliKisiSorguAsync(buyerIdentity, cancellationToken);
                if (kayitliKisi?.KayitliKisiMi != true && buyerIdentity.Length == 11)
                {
                    if (!fatura.CariKart.DogumTarihi.HasValue)
                    {
                        return BadRequest(new
                        {
                            hata = "Cari kartta dogum tarihi olmadan T.C. ile kayitsiz kisi satis kunyesi olusturulamaz."
                        });
                    }
                }

                if (kayitliKisi?.KayitliKisiMi == true && kayitliKisi.SifatIds.Count > 0
                    && !kayitliKisi.SifatIds.Contains(fatura.CariKart.HksSifatId.Value))
                {
                    return BadRequest(new { hata = "Cari kartta secili HKS sifati, HKS kayitli kisi sorgusuyla uyusmuyor." });
                }

                var halIciIsyeriZorunlu = RequiresHalIciIsyeri(seciliIsletmeTuru);
                IReadOnlyList<HksHalIciIsyeriDto> halIciIsyerleri = [];
                if (halIciIsyeriZorunlu)
                {
                    if (!fatura.CariKart.HksHalIciIsyeriId.HasValue || fatura.CariKart.HksHalIciIsyeriId.Value <= 0)
                    {
                        return BadRequest(new { hata = "Cari kartta HKS hal ici isyeri secilmelidir." });
                    }

                    halIciIsyerleri = await _hksService.GetHalIciIsyerleriAsync(buyerIdentity, cancellationToken);
                    if (!halIciIsyerleri.Any(item => item.Id == fatura.CariKart.HksHalIciIsyeriId.Value))
                    {
                        return BadRequest(new { hata = "Cari kartta secili HKS hal ici isyeri, HKS sorgusunda bulunamadi." });
                    }
                }

                var requestItems = new List<HksBildirimKayitItemDto>();

                foreach (var detay in hedefDetaylar)
                {
                    var stok = detay.Stok;
                    if (stok is null)
                    {
                        return BadRequest(new { hata = "Fatura kalemlerinden birine bagli stok bulunamadi." });
                    }

                    if (!stok.HksUrunId.HasValue || stok.HksUrunId.Value <= 0)
                    {
                        return BadRequest(new { hata = $"{stok.StokKodu} - {stok.StokAdi} icin HKS urun tanimi eksik." });
                    }

                    if (!stok.HksUrunCinsiId.HasValue || stok.HksUrunCinsiId.Value <= 0)
                    {
                        return BadRequest(new { hata = $"{stok.StokKodu} - {stok.StokAdi} icin HKS urun cinsi eksik." });
                    }

                    if (!stok.HksUretimSekliId.HasValue || stok.HksUretimSekliId.Value <= 0)
                    {
                        return BadRequest(new { hata = $"{stok.StokKodu} - {stok.StokAdi} icin HKS uretim sekli eksik." });
                    }

                    if (stok.Birim is null)
                    {
                        return BadRequest(new { hata = $"{stok.StokKodu} - {stok.StokAdi} icin birim tanimi eksik." });
                    }

                    var alisKunyeNo = ParseLongOrZero(detay.AlisKunye);
                    if (alisKunyeNo <= 0)
                    {
                        return BadRequest(new { hata = $"{stok.StokKodu} - {stok.StokAdi} icin gecerli bir alis kunyesi secilmelidir." });
                    }

                    var referansKunyeResponse = await _hksService.GetReferansKunyelerAsync(new HksReferansKunyeRequestDto
                    {
                        KunyeNo = alisKunyeNo,
                        UrunId = stok.HksUrunId.Value,
                        KalanMiktariSifirdanBuyukOlanlar = false,
                    }, cancellationToken);

                    var seciliReferansKunye = referansKunyeResponse.ReferansKunyeler
                        .FirstOrDefault(item => item.KunyeNo == alisKunyeNo);

                    if (seciliReferansKunye is null)
                    {
                        return BadRequest(new { hata = $"{stok.StokKodu} - {stok.StokAdi} icin secilen alis kunyesi HKS'de bulunamadi." });
                    }

                    var kalanMiktar = Convert.ToDecimal(seciliReferansKunye.KalanMiktar ?? seciliReferansKunye.MalinMiktari ?? 0d);
                    if (kalanMiktar < detay.Miktar)
                    {
                        return BadRequest(new { hata = $"{stok.StokKodu} - {stok.StokAdi} icin secilen alis kunyesinin kalan miktari yetersiz." });
                    }

                    if (!seciliReferansKunye.UretimIlId.HasValue || seciliReferansKunye.UretimIlId.Value <= 0
                        || !seciliReferansKunye.UretimIlceId.HasValue || seciliReferansKunye.UretimIlceId.Value <= 0
                        || !seciliReferansKunye.UretimBeldeId.HasValue || seciliReferansKunye.UretimBeldeId.Value <= 0)
                    {
                        return BadRequest(new { hata = $"{stok.StokKodu} - {stok.StokAdi} icin alis kunyesinden uretim il / ilce / belde bilgisi alinamadi." });
                    }

                    var miktarBirimId = ResolveHksUrunBirimiId(stok.Birim, hksBirimleri);
                    if (miktarBirimId <= 0)
                    {
                        return BadRequest(new { hata = $"{stok.StokKodu} - {stok.StokAdi} icin HKS urun birimi eslesmedi." });
                    }

                    var referansMiktarBirimId = seciliReferansKunye.MiktarBirimId.GetValueOrDefault();

                    var malinNiteligiId = ResolveMalinNiteligiId(stok.HksNitelik, malinNitelikleri);
                    if (malinNiteligiId <= 0)
                    {
                        return BadRequest(new { hata = $"{stok.StokKodu} - {stok.StokAdi} icin HKS malin niteligi eslesmedi." });
                    }

                    var uniqueId = detay.Id.ToString("N");
                    requestItems.Add(new HksBildirimKayitItemDto
                    {
                        UniqueId = uniqueId,
                        FaturaDetayId = detay.Id,
                        BildirimTuru = dto.BildirimTuruId,
                        ReferansBildirimKunyeNo = alisKunyeNo,
                        BildirimciBilgileri = new HksBildirimciBilgileriDto
                        {
                            KisiSifat = dto.BildirimciSifatId,
                        },
                        IkinciKisiBilgileri = new HksIkinciKisiBilgileriDto
                        {
                            KisiSifat = fatura.CariKart.HksSifatId.Value,
                            TcKimlikVergiNo = buyerIdentity,
                            DogumTarihi = buyerIdentity.Length == 11 && kayitliKisi?.KayitliKisiMi != true
                                ? FormatHksBirthDate(fatura.CariKart.DogumTarihi)
                                : null,
                            AdSoyad = buyerDisplayName,
                            CepTel = buyerPhone,
                            YurtDisiMi = fatura.CariKart.FaturaTipi == FaturaTipiEnum.Ihracat,
                        },
                        MalinGidecekYerBilgileri = new HksMalGidecekYerBilgileriDto
                        {
                            GidecekYerIsletmeTuruId = fatura.CariKart.HksIsletmeTuruId.Value,
                            GidecekIsyeriId = halIciIsyeriZorunlu ? fatura.CariKart.HksHalIciIsyeriId!.Value : null,
                            GidecekUlkeId = null,
                            GidecekYerIlId = fatura.CariKart.HksIlId.Value,
                            GidecekYerIlceId = fatura.CariKart.HksIlceId.Value,
                            GidecekYerBeldeId = fatura.CariKart.HksBeldeId.Value,
                            BelgeNo = belgeNo,
                            BelgeTipi = dto.BelgeTipiId,
                        },
                        BildirimMalBilgileri = new HksBildirimMalBilgileriDto
                        {
                            UretimIlId = seciliReferansKunye.UretimIlId.Value,
                            UretimIlceId = seciliReferansKunye.UretimIlceId.Value,
                            UretimBeldeId = seciliReferansKunye.UretimBeldeId.Value,
                            MalinNiteligi = malinNiteligiId,
                            MalinKodNo = stok.HksUrunId.Value,
                            UretimSekli = stok.HksUretimSekliId.Value,
                            MalinCinsiId = stok.HksUrunCinsiId.Value,
                            MiktarBirimId = referansMiktarBirimId > 0
                                ? referansMiktarBirimId
                                : miktarBirimId,
                            MalinMiktari = Convert.ToDouble(detay.Miktar),
                            MalinSatisFiyat = Convert.ToDouble(detay.BirimFiyat),
                            GelenUlkeId = null,
                            AnalizeGonderilecekMi = false,
                        },
                    });
                }

                var response = await _hksService.SaveBildirimKayitAsync(requestItems, cancellationToken);
                var eslesenSonuclar = response.Sonuclar
                    .Where(item => item.FaturaDetayId.HasValue && item.YeniKunyeNo > 0)
                    .ToDictionary(item => item.FaturaDetayId!.Value, item => item, comparer: EqualityComparer<Guid>.Default);

                foreach (var detay in hedefDetaylar)
                {
                    if (eslesenSonuclar.TryGetValue(detay.Id, out var sonuc))
                    {
                        detay.SatisKunye = sonuc.YeniKunyeNo.ToString();
                    }
                }

                var islenenKalemSayisi = hedefDetaylar.Count(detay => !string.IsNullOrWhiteSpace(detay.SatisKunye));
                if (islenenKalemSayisi == 0)
                {
                    return BadRequest(new { hata = "HKS satis künyesi olusturulamadi." });
                }

                await _context.SaveChangesAsync(cancellationToken);

                return Ok(new FaturaSatisKunyeSonucDto
                {
                    FaturaId = fatura.Id,
                    FaturaNo = fatura.FaturaNo,
                    IslenenKalemSayisi = islenenKalemSayisi,
                    Mesaj = islenenKalemSayisi == hedefDetaylar.Count
                        ? $"{islenenKalemSayisi} kalem icin satis künye olusturuldu."
                        : $"{islenenKalemSayisi}/{hedefDetaylar.Count} kalem icin satis künye olusturuldu.",
                    Kalemler = fatura.Detaylar
                        .OrderBy(d => d.KayitTarihi)
                        .Select(d => new FaturaSatisKunyeKalemSonucDto
                        {
                            DetayId = d.Id,
                            StokId = d.StokId,
                            StokAdi = d.Stok?.StokAdi,
                            AlisKunye = d.AlisKunye,
                            SatisKunye = d.SatisKunye,
                        })
                        .ToList(),
                });
            }
            catch (HksIntegrationException ex)
            {
                return StatusCode(ex.StatusCode, new { hata = ex.Message, ex.IslemKodu, ex.HataKodlari });
            }
        }

        private static string? FormatHksBirthDate(DateTime? value)
        {
            if (!value.HasValue)
            {
                return null;
            }

            return value.Value.Date.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
        }

        private async Task<IActionResult?> ValidateTenantReferencesAsync(Guid musteriId, FaturaDto dto)
        {
            var cariVarMi = await _context.CariKartlar
                .IgnoreQueryFilters()
                .AnyAsync(c => c.Id == dto.CariKartId && c.MusteriId == musteriId && c.AktifMi);

            if (!cariVarMi)
            {
                return BadRequest(new { hata = "Secilen cari kart bulunamadi." });
            }

            var stokIdleri = dto.Kalemler
                .Select(k => k.StokId)
                .Distinct()
                .ToList();

            var stokSayisi = await _context.Stoklar
                .IgnoreQueryFilters()
                .CountAsync(s => stokIdleri.Contains(s.Id) && s.MusteriId == musteriId && s.AktifMi);

            if (stokSayisi != stokIdleri.Count)
            {
                return BadRequest(new { hata = "Fatura kalemlerinde secilen stoklardan biri bulunamadi." });
            }

            return null;
        }

        private async Task<Fatura> BuildInvoiceAsync(Guid musteriId, FaturaDto dto, decimal invoiceTotal, string? invoiceNumberOverride = null)
        {
            var faturaNo = invoiceNumberOverride ?? await GenerateNextInvoiceNumberAsync(musteriId);
            var faturaId = IdGenerator.CreateV7();

            var detaylar = dto.Kalemler.Select(k => new FaturaDetay
            {
                Id = IdGenerator.CreateV7(),
                MusteriId = musteriId,
                FaturaId = faturaId,
                StokId = k.StokId,
                AlisKunye = NormalizeAlisKunye(k.AlisKunye),
                SatisKunye = NormalizeAlisKunye(k.SatisKunye),
                Miktar = k.Miktar,
                BirimFiyat = k.BirimFiyat,
                SatirToplami = Math.Round(k.Miktar * k.BirimFiyat, 2, MidpointRounding.AwayFromZero)
            }).ToList();

            return new Fatura
            {
                Id = faturaId,
                MusteriId = musteriId,
                FaturaNo = faturaNo,
                CariKartId = dto.CariKartId,
                FaturaTarihi = NormalizeInvoiceDate(dto.FaturaTarihi),
                AlisKunye = null,
                Aciklama = string.IsNullOrWhiteSpace(dto.Aciklama) ? null : dto.Aciklama.Trim(),
                Tutar = invoiceTotal,
                TahsilEdilenTutar = NormalizeTahsilatAmount(dto.TahsilEdilenTutar),
                Detaylar = detaylar
            };
        }

        private static decimal CalculateInvoiceTotal(FaturaDto dto)
        {
            return dto.Kalemler.Sum(k => Math.Round(k.Miktar * k.BirimFiyat, 2, MidpointRounding.AwayFromZero));
        }

        private static IActionResult? ValidateTahsilatAmount(decimal value, decimal invoiceTotal)
        {
            if (value < 0)
            {
                return new BadRequestObjectResult(new { hata = "Tahsil edilen tutar negatif olamaz." });
            }

            if (Math.Round(value, 2, MidpointRounding.AwayFromZero) > invoiceTotal)
            {
                return new BadRequestObjectResult(new { hata = "Tahsil edilen tutar fatura toplamindan buyuk olamaz." });
            }

            return null;
        }

        private static decimal NormalizeTahsilatAmount(decimal value)
        {
            return Math.Round(Math.Max(0, value), 2, MidpointRounding.AwayFromZero);
        }

        private async Task<string> GenerateNextInvoiceNumberAsync(Guid musteriId)
        {
            var oncekiNumaralar = await _context.Faturalar
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(f => f.MusteriId == musteriId)
                .Select(f => f.FaturaNo)
                .ToListAsync();

            var siradakiNo = oncekiNumaralar.Count == 0
                ? 1
                : oncekiNumaralar
                    .Select(ParseInvoiceNumber)
                    .DefaultIfEmpty(0)
                    .Max() + 1;

            return $"FTR{siradakiNo:D6}";
        }

        private static int ParseInvoiceNumber(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return 0;
            }

            var match = InvoiceNumberRegex.Match(value);
            return match.Success && int.TryParse(match.Groups[1].Value, out var number)
                ? number
                : 0;
        }

        private IActionResult BuildInvoiceCreateDbErrorResult(DbUpdateException ex)
        {
            if (IsInvoiceNumberConflict(ex))
            {
                return Conflict(new { hata = "Fatura numarasi uretiminde cakisma olustu." });
            }

            if (ex.InnerException is PostgresException postgresException)
            {
                return postgresException.SqlState switch
                {
                    PostgresErrorCodes.ForeignKeyViolation => BadRequest(new
                    {
                        hata = "Baglantili cari veya stok kaydi gecersiz.",
                        detay = postgresException.Detail ?? postgresException.MessageText,
                    }),
                    PostgresErrorCodes.NotNullViolation => BadRequest(new
                    {
                        hata = "Fatura icin zorunlu alanlardan biri bos gonderildi.",
                        detay = postgresException.ColumnName ?? postgresException.MessageText,
                    }),
                    PostgresErrorCodes.StringDataRightTruncation => BadRequest(new
                    {
                        hata = "Fatura alanlarindan biri izin verilen uzunlugu asti.",
                        detay = postgresException.ColumnName ?? postgresException.MessageText,
                    }),
                    PostgresErrorCodes.UniqueViolation => Conflict(new
                    {
                        hata = "Fatura kaydinda benzersizlik hatasi olustu.",
                        detay = postgresException.ConstraintName ?? postgresException.MessageText,
                    }),
                    _ => BadRequest(new
                    {
                        hata = "Fatura kaydedilemedi.",
                        detay = postgresException.MessageText,
                    }),
                };
            }

            return BadRequest(new
            {
                hata = "Fatura kaydedilemedi.",
                detay = ex.InnerException?.Message ?? ex.Message,
            });
        }

        private static bool IsInvoiceNumberConflict(DbUpdateException ex)
        {
            return ex.InnerException is PostgresException postgresException
                && postgresException.SqlState == PostgresErrorCodes.UniqueViolation
                && string.Equals(postgresException.ConstraintName, "IX_Faturalar_MusteriId_FaturaNo_Unique", StringComparison.Ordinal);
        }

        private static string GenerateFallbackInvoiceNumber()
        {
            return $"FTR{DateTime.UtcNow:yyMMddHHmmssfff}";
        }

        private static long GetTenantInvoiceLockKey(Guid musteriId)
        {
            var bytes = musteriId.ToByteArray();
            return BitConverter.ToInt64(bytes, 0);
        }

        private bool IsPostgresProvider()
        {
            return string.Equals(_context.Database.ProviderName, "Npgsql.EntityFrameworkCore.PostgreSQL", StringComparison.Ordinal);
        }

        private static DateTime NormalizeInvoiceDate(DateTime value)
        {
            var date = value == default ? DateTime.UtcNow.Date : value.Date;
            return DateTime.SpecifyKind(date, DateTimeKind.Utc);
        }

        private static DateTime GetIstanbulToday()
        {
            var timeZone = ResolveIstanbulTimeZone();
            return AsUtcDate(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone));
        }

        private static TimeZoneInfo ResolveIstanbulTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
            }
        }

        private static DateTime AsUtcDate(DateTime value)
        {
            return DateTime.SpecifyKind(value.Date, DateTimeKind.Utc);
        }

        private static string? NormalizeAlisKunye(string? value)
        {
            var normalized = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
            if (normalized is null)
            {
                return null;
            }

            return normalized.Length <= 120
                ? normalized
                : normalized[..120];
        }

        private static string ResolveCariDisplayName(CariKart cari)
        {
            if (!string.IsNullOrWhiteSpace(cari.Unvan))
            {
                return cari.Unvan.Trim();
            }

            if (!string.IsNullOrWhiteSpace(cari.AdiSoyadi))
            {
                return cari.AdiSoyadi.Trim();
            }

            return cari.VTCK_No ?? string.Empty;
        }

        private static long ParseLongOrZero(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return 0;
            }

            return long.TryParse(value.Trim(), out var result)
                ? result
                : 0;
        }

        private static string? NormalizeBuyerIdentity(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var digits = new string(value.Where(char.IsDigit).ToArray());
            return digits.Length is 10 or 11 ? digits : null;
        }

        private static string? NormalizeHksMobilePhone(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var digits = new string(value.Where(char.IsDigit).ToArray());
            if (string.IsNullOrWhiteSpace(digits))
            {
                return null;
            }

            if (digits.Length == 12 && digits.StartsWith("90", StringComparison.Ordinal))
            {
                digits = digits[2..];
            }

            if (digits.Length == 11 && digits.StartsWith("0", StringComparison.Ordinal))
            {
                digits = digits[1..];
            }

            return digits.Length == 10 ? digits : null;
        }

        private static bool RequiresHalIciIsyeri(HksSelectOptionDto? isletmeTuru)
        {
            var normalized = NormalizeLookup(isletmeTuru?.Ad);
            return normalized.Contains("HAL ICI", StringComparison.Ordinal)
                || normalized.Contains("HALICI", StringComparison.Ordinal);
        }

        private static int ResolveMalinNiteligiId(string? stokHksNitelik, IReadOnlyList<HksSelectOptionDto> hksMalNitelikleri)
        {
            if (string.IsNullOrWhiteSpace(stokHksNitelik) || hksMalNitelikleri.Count == 0)
            {
                return 0;
            }

            var normalized = NormalizeLookup(stokHksNitelik);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return 0;
            }

            var direct = hksMalNitelikleri.FirstOrDefault(item => NormalizeLookup(item.Ad) == normalized);
            if (direct is not null)
            {
                return direct.Id;
            }

            if (normalized.Contains("YERLI", StringComparison.Ordinal))
            {
                return hksMalNitelikleri
                    .FirstOrDefault(item => NormalizeLookup(item.Ad).Contains("YERLI", StringComparison.Ordinal))
                    ?.Id ?? 0;
            }

            if (normalized.Contains("ITHAL", StringComparison.Ordinal))
            {
                return hksMalNitelikleri
                    .FirstOrDefault(item => NormalizeLookup(item.Ad).Contains("ITHAL", StringComparison.Ordinal))
                    ?.Id ?? 0;
            }

            return 0;
        }

        private static int ResolveHksUrunBirimiId(Birim birim, IReadOnlyList<HksSelectOptionDto> hksBirimleri)
        {
            if (hksBirimleri.Count == 0)
            {
                return 0;
            }

            var sembol = NormalizeLookup(birim.Sembol);
            var ad = NormalizeLookup(birim.Ad);

            var direct = hksBirimleri.FirstOrDefault(item =>
                NormalizeLookup(item.Ad) == sembol
                || NormalizeLookup(item.Ad) == ad
                || (!string.IsNullOrWhiteSpace(sembol) && NormalizeLookup(item.Ad).Contains(sembol, StringComparison.Ordinal))
                || (!string.IsNullOrWhiteSpace(ad) && NormalizeLookup(item.Ad).Contains(ad, StringComparison.Ordinal)));

            if (direct is not null)
            {
                return direct.Id;
            }

            if (sembol.Contains("KG", StringComparison.Ordinal) || ad.Contains("KILO", StringComparison.Ordinal))
            {
                return hksBirimleri
                    .FirstOrDefault(item =>
                        NormalizeLookup(item.Ad).Contains("KILO", StringComparison.Ordinal)
                        || NormalizeLookup(item.Ad).Contains("KG", StringComparison.Ordinal))
                    ?.Id ?? 0;
            }

            if (sembol.Contains("AD", StringComparison.Ordinal) || ad.Contains("ADET", StringComparison.Ordinal))
            {
                return hksBirimleri
                    .FirstOrDefault(item => NormalizeLookup(item.Ad).Contains("ADET", StringComparison.Ordinal))
                    ?.Id ?? 0;
            }

            return 0;
        }

        private static string NormalizeLookup(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return value
                .Trim()
                .ToUpperInvariant()
                .Replace('Ç', 'C')
                .Replace('Ğ', 'G')
                .Replace('İ', 'I')
                .Replace('I', 'I')
                .Replace('Ö', 'O')
                .Replace('Ş', 'S')
                .Replace('Ü', 'U');
        }
    }
}
