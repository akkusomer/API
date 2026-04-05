using AtlasWeb.DTOs;
using AtlasWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AtlasWeb.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public sealed class HksController : ControllerBase
{
    private readonly IHksService _hksService;
    private readonly IHksAyarService _hksAyarService;
    private readonly IHksSifatService _hksSifatService;
    private readonly IHksIlService _hksIlService;
    private readonly IHksIlceService _hksIlceService;
    private readonly IHksBeldeService _hksBeldeService;
    private readonly IHksUrunService _hksUrunService;
    private readonly IHksUrunBirimService _hksUrunBirimService;
    private readonly IHksIsletmeTuruService _hksIsletmeTuruService;
    private readonly IHksUretimSekliService _hksUretimSekliService;
    private readonly IHksUrunCinsiService _hksUrunCinsiService;
    private readonly IHksReferansKunyeKayitService _hksReferansKunyeKayitService;
    private readonly ICurrentUserService _currentUserService;

    public HksController(
        IHksService hksService,
        IHksAyarService hksAyarService,
        IHksSifatService hksSifatService,
        IHksIlService hksIlService,
        IHksIlceService hksIlceService,
        IHksBeldeService hksBeldeService,
        IHksUrunService hksUrunService,
        IHksUrunBirimService hksUrunBirimService,
        IHksIsletmeTuruService hksIsletmeTuruService,
        IHksUretimSekliService hksUretimSekliService,
        IHksUrunCinsiService hksUrunCinsiService,
        IHksReferansKunyeKayitService hksReferansKunyeKayitService,
        ICurrentUserService currentUserService)
    {
        _hksService = hksService;
        _hksAyarService = hksAyarService;
        _hksSifatService = hksSifatService;
        _hksIlService = hksIlService;
        _hksIlceService = hksIlceService;
        _hksBeldeService = hksBeldeService;
        _hksUrunService = hksUrunService;
        _hksUrunBirimService = hksUrunBirimService;
        _hksIsletmeTuruService = hksIsletmeTuruService;
        _hksUretimSekliService = hksUretimSekliService;
        _hksUrunCinsiService = hksUrunCinsiService;
        _hksReferansKunyeKayitService = hksReferansKunyeKayitService;
        _currentUserService = currentUserService;
    }

    [HttpGet("ayarlar")]
    public async Task<IActionResult> GetAyarlar(CancellationToken cancellationToken)
    {
        var denial = EnsureHksAccess();
        if (denial is not null)
        {
            return denial;
        }

        try
        {
            var result = await _hksAyarService.GetCurrentTenantSettingsAsync(cancellationToken);
            return Ok(result);
        }
        catch (HksIntegrationException ex)
        {
            return StatusCode(ex.StatusCode, new { hata = ex.Message, ex.IslemKodu, ex.HataKodlari });
        }
    }

    [HttpPut("ayarlar")]
    public async Task<IActionResult> SaveAyarlar(
        [FromBody] HksAyarKaydetDto request,
        CancellationToken cancellationToken)
    {
        var denial = EnsureHksAccess();
        if (denial is not null)
        {
            return denial;
        }

        try
        {
            var result = await _hksAyarService.SaveCurrentTenantSettingsAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { hata = ex.Message });
        }
        catch (HksIntegrationException ex)
        {
            return StatusCode(ex.StatusCode, new { hata = ex.Message, ex.IslemKodu, ex.HataKodlari });
        }
    }

    [HttpGet("sifatlar")]
    public async Task<IActionResult> GetSifatlar(CancellationToken cancellationToken)
    {
        var denial = EnsureHksAccess();
        if (denial is not null)
        {
            return denial;
        }

        try
        {
            var result = await _hksSifatService.SyncCurrentTenantSifatlarAsync(cancellationToken);
            return Ok(result);
        }
        catch (HksIntegrationException ex)
        {
            return StatusCode(ex.StatusCode, new { hata = ex.Message, ex.IslemKodu, ex.HataKodlari });
        }
    }

    [HttpGet("sifatlar/kayitli")]
    public async Task<IActionResult> GetKayitliSifatlar(CancellationToken cancellationToken)
    {
        var denial = EnsureHksAccess();
        if (denial is not null)
        {
            return denial;
        }

        try
        {
            var result = await _hksSifatService.GetCurrentTenantSifatlarAsync(cancellationToken);
            return Ok(result);
        }
        catch (HksIntegrationException ex)
        {
            return StatusCode(ex.StatusCode, new { hata = ex.Message, ex.IslemKodu, ex.HataKodlari });
        }
    }

    [HttpGet("kayitli-kisi-sorgu")]
    public async Task<IActionResult> GetKayitliKisiSorgu(
        [FromQuery] string? tcKimlikVergiNo,
        CancellationToken cancellationToken)
    {
        var denial = EnsureHksAccess();
        if (denial is not null)
        {
            return denial;
        }

        if (string.IsNullOrWhiteSpace(tcKimlikVergiNo))
        {
            return BadRequest(new { hata = "Tc kimlik veya vergi numarasi girilmelidir." });
        }

        try
        {
            var kayitliKisi = await _hksService.GetKayitliKisiSorguAsync(tcKimlikVergiNo, cancellationToken);
            if (kayitliKisi is null)
            {
                return Ok(new HksKayitliKisiSifatSonucDto
                {
                    TcKimlikVergiNo = tcKimlikVergiNo.Trim(),
                    KayitliKisiMi = false,
                });
            }

            var sifatSozlugu = await _hksSifatService.GetCurrentTenantSifatlarAsync(cancellationToken);
            if (sifatSozlugu.Count == 0)
            {
                sifatSozlugu = await _hksSifatService.SyncCurrentTenantSifatlarAsync(cancellationToken);
            }

            var matchedSifatlar = kayitliKisi.SifatIds
                .Select(id => sifatSozlugu.FirstOrDefault(item => item.HksSifatId == id))
                .Where(item => item is not null)
                .Select(item => new HksSelectOptionDto
                {
                    Id = item!.HksSifatId,
                    Ad = item.Ad
                })
                .ToList();

            var unknownIds = kayitliKisi.SifatIds
                .Where(id => matchedSifatlar.All(item => item.Id != id))
                .Distinct()
                .ToList();

            var halIciIsyerleri = kayitliKisi.KayitliKisiMi
                ? await _hksService.GetHalIciIsyerleriAsync(kayitliKisi.TcKimlikVergiNo, cancellationToken)
                : [];

            return Ok(new HksKayitliKisiSifatSonucDto
            {
                TcKimlikVergiNo = kayitliKisi.TcKimlikVergiNo,
                KayitliKisiMi = kayitliKisi.KayitliKisiMi,
                Sifatlar = matchedSifatlar,
                HalIciIsyerleri = halIciIsyerleri.ToList(),
                BilinmeyenSifatIdler = unknownIds,
            });
        }
        catch (HksIntegrationException ex)
        {
            return StatusCode(ex.StatusCode, new { hata = ex.Message, ex.IslemKodu, ex.HataKodlari });
        }
    }

    [HttpGet("urunler")]
    public async Task<IActionResult> GetUrunler(CancellationToken cancellationToken)
    {
        var denial = EnsureHksAccess();
        if (denial is not null)
        {
            return denial;
        }

        try
        {
            var result = await _hksUrunService.SyncCurrentTenantProductsAsync(cancellationToken);
            return Ok(result);
        }
        catch (HksIntegrationException ex)
        {
            return StatusCode(ex.StatusCode, new { hata = ex.Message, ex.IslemKodu, ex.HataKodlari });
        }
    }

    [HttpGet("iller")]
    public async Task<IActionResult> GetIller(CancellationToken cancellationToken)
    {
        var denial = EnsureHksAccess();
        if (denial is not null)
        {
            return denial;
        }

        try
        {
            var result = await _hksIlService.SyncCurrentTenantCitiesAsync(cancellationToken);
            return Ok(result);
        }
        catch (HksIntegrationException ex)
        {
            return StatusCode(ex.StatusCode, new { hata = ex.Message, ex.IslemKodu, ex.HataKodlari });
        }
    }

    [HttpGet("iller/kayitli")]
    public async Task<IActionResult> GetKayitliIller(CancellationToken cancellationToken)
    {
        var denial = EnsureHksAccess();
        if (denial is not null)
        {
            return denial;
        }

        try
        {
            var result = await _hksIlService.GetCurrentTenantCitiesAsync(cancellationToken);
            return Ok(result);
        }
        catch (HksIntegrationException ex)
        {
            return StatusCode(ex.StatusCode, new { hata = ex.Message, ex.IslemKodu, ex.HataKodlari });
        }
    }

    [HttpGet("ilceler")]
    public async Task<IActionResult> GetIlceler([FromQuery] int? ilId, CancellationToken cancellationToken)
    {
        var denial = EnsureHksAccess();
        if (denial is not null)
        {
            return denial;
        }

        try
        {
            var result = await _hksIlceService.SyncCurrentTenantDistrictsAsync(ilId, cancellationToken);
            return Ok(result);
        }
        catch (HksIntegrationException ex)
        {
            return StatusCode(ex.StatusCode, new { hata = ex.Message, ex.IslemKodu, ex.HataKodlari });
        }
    }

    [HttpGet("ilceler/kayitli")]
    public async Task<IActionResult> GetKayitliIlceler([FromQuery] int? ilId, CancellationToken cancellationToken)
    {
        var denial = EnsureHksAccess();
        if (denial is not null)
        {
            return denial;
        }

        try
        {
            var result = await _hksIlceService.GetCurrentTenantDistrictsAsync(ilId, cancellationToken);
            return Ok(result);
        }
        catch (HksIntegrationException ex)
        {
            return StatusCode(ex.StatusCode, new { hata = ex.Message, ex.IslemKodu, ex.HataKodlari });
        }
    }

    [HttpGet("beldeler")]
    public async Task<IActionResult> GetBeldeler([FromQuery] int? ilceId, CancellationToken cancellationToken)
    {
        var denial = EnsureHksAccess();
        if (denial is not null)
        {
            return denial;
        }

        try
        {
            var result = await _hksBeldeService.SyncCurrentTenantTownsAsync(ilceId, cancellationToken);
            return Ok(result);
        }
        catch (HksIntegrationException ex)
        {
            return StatusCode(ex.StatusCode, new { hata = ex.Message, ex.IslemKodu, ex.HataKodlari });
        }
    }

    [HttpGet("beldeler/kayitli")]
    public async Task<IActionResult> GetKayitliBeldeler([FromQuery] int? ilceId, CancellationToken cancellationToken)
    {
        var denial = EnsureHksAccess();
        if (denial is not null)
        {
            return denial;
        }

        try
        {
            var result = await _hksBeldeService.GetCurrentTenantTownsAsync(ilceId, cancellationToken);
            return Ok(result);
        }
        catch (HksIntegrationException ex)
        {
            return StatusCode(ex.StatusCode, new { hata = ex.Message, ex.IslemKodu, ex.HataKodlari });
        }
    }

    [HttpGet("urunler/kayitli")]
    public async Task<IActionResult> GetKayitliUrunler(CancellationToken cancellationToken)
    {
        var denial = EnsureHksAccess();
        if (denial is not null)
        {
            return denial;
        }

        try
        {
            var result = await _hksUrunService.GetCurrentTenantProductsAsync(cancellationToken);
            return Ok(result);
        }
        catch (HksIntegrationException ex)
        {
            return StatusCode(ex.StatusCode, new { hata = ex.Message, ex.IslemKodu, ex.HataKodlari });
        }
    }

    [HttpGet("urun-birimleri")]
    public async Task<IActionResult> GetUrunBirimleri(CancellationToken cancellationToken)
    {
        var denial = EnsureHksAccess();
        if (denial is not null)
        {
            return denial;
        }

        try
        {
            var result = await _hksUrunBirimService.SyncCurrentTenantProductUnitsAsync(cancellationToken);
            return Ok(result);
        }
        catch (HksIntegrationException ex)
        {
            return StatusCode(ex.StatusCode, new { hata = ex.Message, ex.IslemKodu, ex.HataKodlari });
        }
    }

    [HttpGet("urun-birimleri/kayitli")]
    public async Task<IActionResult> GetKayitliUrunBirimleri(CancellationToken cancellationToken)
    {
        var denial = EnsureHksAccess();
        if (denial is not null)
        {
            return denial;
        }

        try
        {
            var result = await _hksUrunBirimService.GetCurrentTenantProductUnitsAsync(cancellationToken);
            return Ok(result);
        }
        catch (HksIntegrationException ex)
        {
            return StatusCode(ex.StatusCode, new { hata = ex.Message, ex.IslemKodu, ex.HataKodlari });
        }
    }

    [HttpGet("isletme-turleri")]
    public async Task<IActionResult> GetIsletmeTurleri(CancellationToken cancellationToken)
    {
        var denial = EnsureHksAccess();
        if (denial is not null)
        {
            return denial;
        }

        try
        {
            var result = await _hksIsletmeTuruService.SyncCurrentTenantBusinessTypesAsync(cancellationToken);
            return Ok(result);
        }
        catch (HksIntegrationException ex)
        {
            return StatusCode(ex.StatusCode, new { hata = ex.Message, ex.IslemKodu, ex.HataKodlari });
        }
    }

    [HttpGet("isletme-turleri/kayitli")]
    public async Task<IActionResult> GetKayitliIsletmeTurleri(CancellationToken cancellationToken)
    {
        var denial = EnsureHksAccess();
        if (denial is not null)
        {
            return denial;
        }

        try
        {
            var result = await _hksIsletmeTuruService.GetCurrentTenantBusinessTypesAsync(cancellationToken);
            return Ok(result);
        }
        catch (HksIntegrationException ex)
        {
            return StatusCode(ex.StatusCode, new { hata = ex.Message, ex.IslemKodu, ex.HataKodlari });
        }
    }

    [HttpGet("uretim-sekilleri")]
    public async Task<IActionResult> GetUretimSekilleri(CancellationToken cancellationToken)
    {
        var denial = EnsureHksAccess();
        if (denial is not null)
        {
            return denial;
        }

        try
        {
            var result = await _hksUretimSekliService.SyncCurrentTenantProductionShapesAsync(cancellationToken);
            return Ok(result);
        }
        catch (HksIntegrationException ex)
        {
            return StatusCode(ex.StatusCode, new { hata = ex.Message, ex.IslemKodu, ex.HataKodlari });
        }
    }

    [HttpGet("uretim-sekilleri/kayitli")]
    public async Task<IActionResult> GetKayitliUretimSekilleri(CancellationToken cancellationToken)
    {
        var denial = EnsureHksAccess();
        if (denial is not null)
        {
            return denial;
        }

        try
        {
            var result = await _hksUretimSekliService.GetCurrentTenantProductionShapesAsync(cancellationToken);
            return Ok(result);
        }
        catch (HksIntegrationException ex)
        {
            return StatusCode(ex.StatusCode, new { hata = ex.Message, ex.IslemKodu, ex.HataKodlari });
        }
    }

    [HttpGet("bildirim-turleri")]
    public async Task<IActionResult> GetBildirimTurleri(CancellationToken cancellationToken)
    {
        var denial = EnsureHksAccess();
        if (denial is not null)
        {
            return denial;
        }

        try
        {
            var result = await _hksService.GetBildirimTurleriAsync(cancellationToken);
            return Ok(result);
        }
        catch (HksIntegrationException ex)
        {
            return StatusCode(ex.StatusCode, new { hata = ex.Message, ex.IslemKodu, ex.HataKodlari });
        }
    }

    [HttpGet("belge-tipleri")]
    public async Task<IActionResult> GetBelgeTipleri(CancellationToken cancellationToken)
    {
        var denial = EnsureHksAccess();
        if (denial is not null)
        {
            return denial;
        }

        try
        {
            var result = await _hksService.GetBelgeTipleriAsync(cancellationToken);
            return Ok(result);
        }
        catch (HksIntegrationException ex)
        {
            return StatusCode(ex.StatusCode, new { hata = ex.Message, ex.IslemKodu, ex.HataKodlari });
        }
    }

    [HttpGet("urun-cinsleri")]
    public async Task<IActionResult> GetUrunCinsleri([FromQuery] int? urunId, CancellationToken cancellationToken)
    {
        var denial = EnsureHksAccess();
        if (denial is not null)
        {
            return denial;
        }

        try
        {
            var result = await _hksUrunCinsiService.SyncCurrentTenantProductKindsAsync(urunId, cancellationToken);
            return Ok(result);
        }
        catch (HksIntegrationException ex)
        {
            return StatusCode(ex.StatusCode, new { hata = ex.Message, ex.IslemKodu, ex.HataKodlari });
        }
    }

    [HttpGet("urun-cinsleri/kayitli")]
    public async Task<IActionResult> GetKayitliUrunCinsleri([FromQuery] int? urunId, CancellationToken cancellationToken)
    {
        var denial = EnsureHksAccess();
        if (denial is not null)
        {
            return denial;
        }

        try
        {
            var result = await _hksUrunCinsiService.GetCurrentTenantProductKindsAsync(urunId, cancellationToken);
            return Ok(result);
        }
        catch (HksIntegrationException ex)
        {
            return StatusCode(ex.StatusCode, new { hata = ex.Message, ex.IslemKodu, ex.HataKodlari });
        }
    }

    [HttpPost("referans-kunyeler")]
    public async Task<IActionResult> GetReferansKunyeler(
        [FromBody] HksReferansKunyeRequestDto request,
        CancellationToken cancellationToken)
    {
        var denial = EnsureHksAccess();
        if (denial is not null)
        {
            return denial;
        }

        if (request.BaslangicTarihi.HasValue
            && request.BitisTarihi.HasValue
            && request.BaslangicTarihi > request.BitisTarihi)
        {
            return BadRequest(new { hata = "Baslangic tarihi bitis tarihinden buyuk olamaz." });
        }

        try
        {
            var result = await _hksReferansKunyeKayitService.QueueCurrentTenantSearchAsync(request, cancellationToken);
            return Accepted(result);
        }
        catch (HksIntegrationException ex)
        {
            return StatusCode(ex.StatusCode, new { hata = ex.Message, ex.IslemKodu, ex.HataKodlari });
        }
    }

    [HttpPost("referans-kunyeler/anlik")]
    public async Task<IActionResult> GetReferansKunyelerAnlik(
        [FromBody] HksReferansKunyeRequestDto request,
        CancellationToken cancellationToken)
    {
        var denial = EnsureHksAccess();
        if (denial is not null)
        {
            return denial;
        }

        if (request.BaslangicTarihi.HasValue
            && request.BitisTarihi.HasValue
            && request.BaslangicTarihi > request.BitisTarihi)
        {
            return BadRequest(new { hata = "Baslangic tarihi bitis tarihinden buyuk olamaz." });
        }

        try
        {
            var result = await _hksService.GetReferansKunyelerForTenantAsync(
                _currentUserService.MusteriId!.Value,
                request,
                progressCallback: null,
                cancellationToken);

            return Ok(result);
        }
        catch (HksIntegrationException ex)
        {
            return StatusCode(ex.StatusCode, new { hata = ex.Message, ex.IslemKodu, ex.HataKodlari });
        }
    }

    [HttpGet("referans-kunyeler/kayitli")]
    public async Task<IActionResult> GetKayitliReferansKunyeler(CancellationToken cancellationToken)
    {
        var denial = EnsureHksAccess();
        if (denial is not null)
        {
            return denial;
        }

        try
        {
            var result = await _hksReferansKunyeKayitService.GetCurrentTenantSnapshotAsync(cancellationToken);
            return result is null ? NoContent() : Ok(result);
        }
        catch (HksIntegrationException ex)
        {
            return StatusCode(ex.StatusCode, new { hata = ex.Message, ex.IslemKodu, ex.HataKodlari });
        }
    }

    [HttpPut("referans-kunyeler/kayitli")]
    public async Task<IActionResult> SaveKayitliReferansKunyeler(
        [FromBody] HksReferansKunyeKaydetDto request,
        CancellationToken cancellationToken)
    {
        var denial = EnsureHksAccess();
        if (denial is not null)
        {
            return denial;
        }

        if (request.BaslangicTarihi.HasValue
            && request.BitisTarihi.HasValue
            && request.BaslangicTarihi > request.BitisTarihi)
        {
            return BadRequest(new { hata = "Baslangic tarihi bitis tarihinden buyuk olamaz." });
        }

        try
        {
            var result = await _hksReferansKunyeKayitService.SaveCurrentTenantSnapshotAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (HksIntegrationException ex)
        {
            return StatusCode(ex.StatusCode, new { hata = ex.Message, ex.IslemKodu, ex.HataKodlari });
        }
    }

    private IActionResult? EnsureHksAccess()
    {
        if (_currentUserService.MusteriId is null || _currentUserService.MusteriId == Guid.Empty)
        {
            return Unauthorized(new { hata = "HKS ekranina erismek icin aktif sirket baglantisi gerekir." });
        }

        return _currentUserService.IsSystemAdmin
            ? Forbid()
            : null;
    }
}
