using System.Globalization;
using System.Net.Mime;
using System.Text;
using System.Xml.Linq;
using AtlasWeb.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace AtlasWeb.Services;

public sealed class HksService : IHksService
{
    private const string SoapEnvelopeNamespace = "http://schemas.xmlsoap.org/soap/envelope/";
    private const string ServiceNamespace = "http://www.gtb.gov.tr//WebServices";
    private const string BildirimContractNamespace = "http://schemas.datacontract.org/2004/07/GTB.HKS.Bildirim.ServiceContract";
    private const string BildirimModelNamespace = "http://schemas.datacontract.org/2004/07/GTB.HKS.Bildirim.Model";
    private const string GenelContractNamespace = "http://schemas.datacontract.org/2004/07/GTB.HKS.Genel.ServiceContract";
    private const string UrunContractNamespace = "http://schemas.datacontract.org/2004/07/GTB.HKS.Urun.ServiceContract";
    private const string ArrayNamespace = "http://schemas.microsoft.com/2003/10/Serialization/Arrays";
    private const string SuccessIslemKodu = "GTBWSRV0000001";

    private readonly HttpClient _httpClient;
    private readonly HksOptions _options;
    private readonly IHksAyarService _hksAyarService;
    private readonly ILogger<HksService> _logger;

    public HksService(
        HttpClient httpClient,
        IOptions<HksOptions> options,
        IHksAyarService hksAyarService,
        ILogger<HksService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _hksAyarService = hksAyarService;
        _logger = logger;

        _httpClient.Timeout = TimeSpan.FromSeconds(Math.Clamp(_options.TimeoutSeconds, 10, 180));
    }

    public async Task<IReadOnlyList<HksSelectOptionDto>> GetSifatlarAsync(CancellationToken cancellationToken = default)
    {
        var credentials = await _hksAyarService.GetCurrentTenantCredentialsAsync(cancellationToken);
        var document = await SendSoapRequestAsync(
            _options.BildirimEndpoint,
            "http://www.gtb.gov.tr//WebServices/IBildirimService/BildirimServisSifatListesi",
            BuildBaseRequest("BaseRequestMessageOf_SifatIstek", BuildEmptyRequest(), credentials),
            cancellationToken);

        var baseResponse = ParseBaseResponse(document);
        EnsureBusinessSuccess(baseResponse, "HKS sifat listesi alinamadi.");

        return ParseOptions(
            FindDescendant(baseResponse.SonucElement, "Sifatlar"),
            "SifatDTO",
            "SifatAdi",
            "Adi",
            "Sifat");
    }

    public async Task<HksKayitliKisiSorguDto?> GetKayitliKisiSorguAsync(
        string tcKimlikVergiNo,
        CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(tcKimlikVergiNo);
        if (normalized is null)
        {
            throw new HksIntegrationException(
                "Tc kimlik veya vergi numarasi bos olamaz.",
                StatusCodes.Status400BadRequest);
        }

        var credentials = await _hksAyarService.GetCurrentTenantCredentialsAsync(cancellationToken);
        var document = await SendSoapRequestAsync(
            _options.BildirimEndpoint,
            "http://www.gtb.gov.tr//WebServices/IBildirimService/BildirimServisKayitliKisiSorgu",
            BuildBaseRequest(
                "BaseRequestMessageOf_KayitliKisiSorguIstek",
                BuildStringArrayRequest("TcKimlikVergiNolar", [normalized], BildirimContractNamespace),
                credentials),
            cancellationToken);

        var baseResponse = ParseBaseResponse(document);
        EnsureBusinessSuccess(baseResponse, "HKS kayitli kisi sorgusu alinamadi.");

        return ParseKayitliKisiSorgu(baseResponse.SonucElement)
            .FirstOrDefault(item => string.Equals(item.TcKimlikVergiNo, normalized, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<IReadOnlyList<HksHalIciIsyeriDto>> GetHalIciIsyerleriAsync(
        string tcKimlikVergiNo,
        CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(tcKimlikVergiNo);
        if (normalized is null)
        {
            throw new HksIntegrationException(
                "Tc kimlik veya vergi numarasi bos olamaz.",
                StatusCodes.Status400BadRequest);
        }

        var credentials = await _hksAyarService.GetCurrentTenantCredentialsAsync(cancellationToken);
        var document = await SendSoapRequestAsync(
            _options.GenelEndpoint,
            "http://www.gtb.gov.tr//WebServices/IGenelService/GenelServisHalIciIsyeri",
            BuildBaseRequest(
                "BaseRequestMessageOf_HalIciIsyeriIstek",
                BuildSingleStringRequest("TcKimlikVergiNo", normalized, GenelContractNamespace),
                credentials),
            cancellationToken);

        var baseResponse = ParseBaseResponse(document);
        EnsureBusinessSuccess(baseResponse, "HKS hal ici isyeri listesi alinamadi.");

        return ParseHalIciIsyerleri(baseResponse.SonucElement, normalized);
    }

    public async Task<IReadOnlyList<HksSelectOptionDto>> GetIllerAsync(CancellationToken cancellationToken = default)
    {
        var credentials = await _hksAyarService.GetCurrentTenantCredentialsAsync(cancellationToken);
        return await GetIllerAsync(credentials, cancellationToken);
    }

    public async Task<IReadOnlyList<HksSelectOptionDto>> GetIllerForTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var credentials = await _hksAyarService.GetTenantCredentialsAsync(tenantId, cancellationToken);
        return await GetIllerAsync(credentials, cancellationToken);
    }

    private async Task<IReadOnlyList<HksSelectOptionDto>> GetIllerAsync(
        HksCredentialSet credentials,
        CancellationToken cancellationToken)
    {
        var document = await SendSoapRequestAsync(
            _options.GenelEndpoint,
            "http://www.gtb.gov.tr//WebServices/IGenelService/GenelServisIller",
            BuildBaseRequest("BaseRequestMessageOf_IllerIstek", BuildEmptyRequest(), credentials),
            cancellationToken);

        var baseResponse = ParseBaseResponse(document);
        EnsureBusinessSuccess(baseResponse, "HKS il listesi alinamadi.");

        return ParseOptions(
            FindDescendant(baseResponse.SonucElement, "Iller"),
            "IlDTO",
            "IlAdi",
            "Adi",
            "Il");
    }

    public async Task<IReadOnlyList<HksSelectOptionDto>> GetIlcelerAsync(int ilId, CancellationToken cancellationToken = default)
    {
        var credentials = await _hksAyarService.GetCurrentTenantCredentialsAsync(cancellationToken);
        return await GetIlcelerAsync(credentials, ilId, cancellationToken);
    }

    public async Task<IReadOnlyList<HksSelectOptionDto>> GetIlcelerForTenantAsync(Guid tenantId, int ilId, CancellationToken cancellationToken = default)
    {
        var credentials = await _hksAyarService.GetTenantCredentialsAsync(tenantId, cancellationToken);
        return await GetIlcelerAsync(credentials, ilId, cancellationToken);
    }

    private async Task<IReadOnlyList<HksSelectOptionDto>> GetIlcelerAsync(
        HksCredentialSet credentials,
        int ilId,
        CancellationToken cancellationToken)
    {
        var document = await SendSoapRequestAsync(
            _options.GenelEndpoint,
            "http://www.gtb.gov.tr//WebServices/IGenelService/GenelServisIlceler",
            BuildBaseRequest(
                "BaseRequestMessageOf_IlcelerIstek",
                BuildSingleIntRequest("IlId", ilId, GenelContractNamespace),
                credentials),
            cancellationToken);

        var baseResponse = ParseBaseResponse(document);
        EnsureBusinessSuccess(baseResponse, "HKS ilce listesi alinamadi.");

        return ParseOptions(
            FindDescendant(baseResponse.SonucElement, "Ilceler"),
            "IlceDTO",
            "IlceAdi",
            "Adi",
            "Ilce");
    }

    public async Task<IReadOnlyList<HksSelectOptionDto>> GetBeldelerAsync(int ilceId, CancellationToken cancellationToken = default)
    {
        var credentials = await _hksAyarService.GetCurrentTenantCredentialsAsync(cancellationToken);
        return await GetBeldelerAsync(credentials, ilceId, cancellationToken);
    }

    public async Task<IReadOnlyList<HksSelectOptionDto>> GetBeldelerForTenantAsync(Guid tenantId, int ilceId, CancellationToken cancellationToken = default)
    {
        var credentials = await _hksAyarService.GetTenantCredentialsAsync(tenantId, cancellationToken);
        return await GetBeldelerAsync(credentials, ilceId, cancellationToken);
    }

    private async Task<IReadOnlyList<HksSelectOptionDto>> GetBeldelerAsync(
        HksCredentialSet credentials,
        int ilceId,
        CancellationToken cancellationToken)
    {
        var document = await SendSoapRequestAsync(
            _options.GenelEndpoint,
            "http://www.gtb.gov.tr//WebServices/IGenelService/GenelServisBeldeler",
            BuildBaseRequest(
                "BaseRequestMessageOf_BeldelerIstek",
                BuildSingleIntRequest("IlceId", ilceId, GenelContractNamespace),
                credentials),
            cancellationToken);

        var baseResponse = ParseBaseResponse(document);
        EnsureBusinessSuccess(baseResponse, "HKS belde listesi alinamadi.");

        return ParseOptions(
            FindDescendant(baseResponse.SonucElement, "Beldeler"),
            "BeldeDTO",
            "BeldeAdi",
            "Adi",
            "Belde");
    }

    public async Task<IReadOnlyList<HksSelectOptionDto>> GetUrunlerAsync(CancellationToken cancellationToken = default)
    {
        var credentials = await _hksAyarService.GetCurrentTenantCredentialsAsync(cancellationToken);
        return await GetUrunlerAsync(credentials, cancellationToken);
    }

    private async Task<IReadOnlyList<HksSelectOptionDto>> GetUrunlerAsync(
        HksCredentialSet credentials,
        CancellationToken cancellationToken)
    {
        var document = await SendSoapRequestAsync(
            _options.UrunEndpoint,
            "http://www.gtb.gov.tr//WebServices/IUrunService/UrunServiceUrunler",
            BuildBaseRequest("BaseRequestMessageOf_UrunlerIstek", BuildEmptyRequest(), credentials),
            cancellationToken);

        var baseResponse = ParseBaseResponse(document);
        EnsureBusinessSuccess(baseResponse, "HKS urun listesi alinamadi.");

        return ParseOptions(
            FindDescendant(baseResponse.SonucElement, "Urunler"),
            "UrunDTO",
            "UrunAdi",
            "Adi");
    }

    public async Task<IReadOnlyList<HksSelectOptionDto>> GetUrunBirimleriAsync(CancellationToken cancellationToken = default)
    {
        var credentials = await _hksAyarService.GetCurrentTenantCredentialsAsync(cancellationToken);
        var document = await SendSoapRequestAsync(
            _options.UrunEndpoint,
            "http://www.gtb.gov.tr//WebServices/IUrunService/UrunServiceUrunBirimleri",
            BuildBaseRequest("BaseRequestMessageOf_UrunBirimleriIstek", BuildEmptyRequest(), credentials),
            cancellationToken);

        var baseResponse = ParseBaseResponse(document);
        EnsureBusinessSuccess(baseResponse, "HKS urun birimi listesi alinamadi.");

        return ParseOptions(
            FindDescendant(baseResponse.SonucElement, "UrunBirimleri"),
            "UrunBirimiDTO",
            "UrunBirimAdi",
            "Adi");
    }

    public async Task<IReadOnlyList<HksSelectOptionDto>> GetIsletmeTurleriAsync(CancellationToken cancellationToken = default)
    {
        var credentials = await _hksAyarService.GetCurrentTenantCredentialsAsync(cancellationToken);
        var document = await SendSoapRequestAsync(
            _options.GenelEndpoint,
            "http://www.gtb.gov.tr//WebServices/IGenelService/GenelServisIsletmeTurleri",
            BuildBaseRequest("BaseRequestMessageOf_IsletmeTurleriIstek", BuildEmptyRequest(), credentials),
            cancellationToken);

        var baseResponse = ParseBaseResponse(document);
        EnsureBusinessSuccess(baseResponse, "HKS isletme turu listesi alinamadi.");

        return ParseOptions(
            FindDescendant(baseResponse.SonucElement, "IsletmeTurleri"),
            "IsletmeTuruDTO",
            "IsletmeTuruAdi",
            "Adi");
    }

    public async Task<IReadOnlyList<HksSelectOptionDto>> GetUretimSekilleriAsync(CancellationToken cancellationToken = default)
    {
        var credentials = await _hksAyarService.GetCurrentTenantCredentialsAsync(cancellationToken);
        var document = await SendSoapRequestAsync(
            _options.UrunEndpoint,
            "http://www.gtb.gov.tr//WebServices/IUrunService/UrunServiceUretimSekilleri",
            BuildBaseRequest("BaseRequestMessageOf_UretimSekilleriIstek", BuildEmptyRequest(), credentials),
            cancellationToken);

        var baseResponse = ParseBaseResponse(document);
        EnsureBusinessSuccess(baseResponse, "HKS uretim sekli listesi alinamadi.");

        return ParseOptions(
            FindDescendant(baseResponse.SonucElement, "UretimSekilleri"),
            "UretimSekliDTO",
            "UretimSekliAdi",
            "Adi");
    }

    public async Task<IReadOnlyList<HksSelectOptionDto>> GetBildirimTurleriAsync(CancellationToken cancellationToken = default)
    {
        var credentials = await _hksAyarService.GetCurrentTenantCredentialsAsync(cancellationToken);
        var document = await SendSoapRequestAsync(
            _options.BildirimEndpoint,
            "http://www.gtb.gov.tr//WebServices/IBildirimService/BildirimServisBildirimTurleri",
            BuildBaseRequest("BaseRequestMessageOf_BildirimTurleriIstek", BuildEmptyRequest(), credentials),
            cancellationToken);

        var baseResponse = ParseBaseResponse(document);
        EnsureBusinessSuccess(baseResponse, "HKS bildirim turu listesi alinamadi.");

        return ParseOptions(
            FindDescendant(baseResponse.SonucElement, "BildirimTurleri"),
            "BildirimTuruDTO",
            "BildirimTuruAdi",
            "Adi");
    }

    public async Task<IReadOnlyList<HksSelectOptionDto>> GetBelgeTipleriAsync(CancellationToken cancellationToken = default)
    {
        var credentials = await _hksAyarService.GetCurrentTenantCredentialsAsync(cancellationToken);
        var document = await SendSoapRequestAsync(
            _options.BildirimEndpoint,
            "http://www.gtb.gov.tr//WebServices/IBildirimService/BildirimServisBelgeTipleriListesi",
            BuildBaseRequest("BaseRequestMessageOf_BelgeTipleriIstek", BuildEmptyRequest(), credentials),
            cancellationToken);

        var baseResponse = ParseBaseResponse(document);
        EnsureBusinessSuccess(baseResponse, "HKS belge tipi listesi alinamadi.");

        return ParseOptions(
            FindDescendant(baseResponse.SonucElement, "BelgeTipleri"),
            "BelgeTipDTO",
            "BelgeTipiAdi",
            "BelgeTipAdi",
            "Adi");
    }

    public async Task<IReadOnlyList<HksSelectOptionDto>> GetMalinNitelikleriAsync(CancellationToken cancellationToken = default)
    {
        var credentials = await _hksAyarService.GetCurrentTenantCredentialsAsync(cancellationToken);
        var document = await SendSoapRequestAsync(
            _options.UrunEndpoint,
            "http://www.gtb.gov.tr//WebServices/IUrunService/UrunServiceMalinNiteligi",
            BuildBaseRequest("BaseRequestMessageOf_MalinNiteligiIstek", BuildEmptyRequest(), credentials),
            cancellationToken);

        var baseResponse = ParseBaseResponse(document);
        EnsureBusinessSuccess(baseResponse, "HKS malin niteligi listesi alinamadi.");

        return ParseOptions(
            FindDescendant(baseResponse.SonucElement, "MalinNitelikleri"),
            "MalinNiteligiDTO",
            "MalinNiteligiAdi",
            "Adi");
    }

    public async Task<IReadOnlyList<HksUrunCinsiDto>> GetUrunCinsleriAsync(int urunId, CancellationToken cancellationToken = default)
    {
        var credentials = await _hksAyarService.GetCurrentTenantCredentialsAsync(cancellationToken);
        var document = await SendSoapRequestAsync(
            _options.UrunEndpoint,
            "http://www.gtb.gov.tr//WebServices/IUrunService/UrunServiceUrunCinsleri",
            BuildBaseRequest(
                "BaseRequestMessageOf_UrunCinsleriIstek",
                BuildSingleIntRequest("UrunId", urunId, UrunContractNamespace),
                credentials),
            cancellationToken);

        var baseResponse = ParseBaseResponse(document);
        EnsureBusinessSuccess(baseResponse, "HKS urun cinsi listesi alinamadi.");

        return ParseUrunCinsleri(baseResponse.SonucElement);
    }

    public async Task<HksBildirimKayitResponseDto> SaveBildirimKayitAsync(
        IReadOnlyList<HksBildirimKayitItemDto> request,
        CancellationToken cancellationToken = default)
    {
        if (request.Count == 0)
        {
            throw new HksIntegrationException(
                "Satış künyesi için gönderilecek kalem bulunamadi.",
                StatusCodes.Status400BadRequest);
        }

        var credentials = await _hksAyarService.GetCurrentTenantCredentialsAsync(cancellationToken);
        var document = await SendSoapRequestAsync(
            _options.BildirimEndpoint,
            "http://www.gtb.gov.tr//WebServices/IBildirimService/BildirimServisBildirimKaydet",
            BuildBaseRequest(
                "BaseRequestMessageOf_ListOf_BildirimKayitIstek",
                BuildBildirimKayitRequest(request),
                credentials),
            cancellationToken);

        var baseResponse = ParseBaseResponse(document);
        EnsureBusinessSuccess(baseResponse, "HKS satis künye bildirimi kaydedilemedi.");

        return new HksBildirimKayitResponseDto
        {
            IslemKodu = baseResponse.IslemKodu,
            HataKodu = GetNullableInt(baseResponse.SonucElement, "HataKodu"),
            Mesaj = GetString(baseResponse.SonucElement, "Mesaj"),
            HataKodlari = baseResponse.HataKodlari.ToList(),
            Sonuclar = ParseBildirimKayitSonuclari(baseResponse.SonucElement, request),
        };
    }

    public async Task<HksReferansKunyeResponseDto> GetReferansKunyelerAsync(
        HksReferansKunyeRequestDto request,
        CancellationToken cancellationToken = default)
    {
        return await GetReferansKunyelerInternalAsync(
            request,
            credentials: null,
            tenantId: null,
            progressCallback: null,
            cancellationToken);
    }

    public async Task<HksReferansKunyeResponseDto> GetReferansKunyelerForTenantAsync(
        Guid tenantId,
        HksReferansKunyeRequestDto request,
        Func<HksSearchProgressDto, Task>? progressCallback = null,
        CancellationToken cancellationToken = default)
    {
        return await GetReferansKunyelerInternalAsync(
            request,
            credentials: null,
            tenantId,
            progressCallback,
            cancellationToken);
    }

    private async Task<HksReferansKunyeResponseDto> GetReferansKunyelerInternalAsync(
        HksReferansKunyeRequestDto request,
        HksCredentialSet? credentials,
        Guid? tenantId,
        Func<HksSearchProgressDto, Task>? progressCallback,
        CancellationToken cancellationToken)
    {
        credentials ??= tenantId.HasValue
            ? await _hksAyarService.GetTenantCredentialsAsync(tenantId.Value, cancellationToken)
            : await _hksAyarService.GetCurrentTenantCredentialsAsync(cancellationToken);
        var resolvedRequest = new HksReferansKunyeRequestDto
        {
            MalinSahibiTcKimlikVergiNo = Normalize(request.MalinSahibiTcKimlikVergiNo) ?? credentials.UserName,
            KunyeNo = request.KunyeNo,
            KalanMiktariSifirdanBuyukOlanlar = request.KalanMiktariSifirdanBuyukOlanlar,
            BaslangicTarihi = request.BaslangicTarihi,
            BitisTarihi = request.BitisTarihi,
            KisiSifat = request.KisiSifat,
            UrunId = request.UrunId,
        };
        var products = resolvedRequest.UrunId.HasValue && resolvedRequest.UrunId > 0
            ? [new HksSelectOptionDto { Id = resolvedRequest.UrunId.Value, Ad = $"Urun {resolvedRequest.UrunId.Value}" }]
            : (await GetUrunlerAsync(credentials, cancellationToken))
                .Where(item => item.Id > 0)
                .ToList();
        var aggregated = new Dictionary<string, HksReferansKunyeDto>(StringComparer.OrdinalIgnoreCase);
        var requestSegments = BuildDateSegments(resolvedRequest);
        var totalSteps = requestSegments.Count * products.Count;
        var completedSteps = 0;

        if (progressCallback is not null)
        {
            await progressCallback(new HksSearchProgressDto
            {
                CompletedSteps = 0,
                TotalSteps = totalSteps,
                ProgressPercent = totalSteps > 0 ? 1 : 0,
                Label = "HKS sorgusu baslatildi"
            });
        }

        foreach (var segment in requestSegments)
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var product in products)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var response = await GetReferansKunyelerByProductAsync(
                    new HksReferansKunyeRequestDto
                    {
                        MalinSahibiTcKimlikVergiNo = segment.MalinSahibiTcKimlikVergiNo,
                        KunyeNo = segment.KunyeNo,
                        KalanMiktariSifirdanBuyukOlanlar = segment.KalanMiktariSifirdanBuyukOlanlar,
                        BaslangicTarihi = segment.BaslangicTarihi,
                        BitisTarihi = segment.BitisTarihi,
                        KisiSifat = segment.KisiSifat,
                        UrunId = product.Id,
                    },
                    credentials,
                    cancellationToken);

                foreach (var referansKunye in response.ReferansKunyeler)
                {
                    var key = !string.IsNullOrWhiteSpace(referansKunye.UniqueId)
                        ? referansKunye.UniqueId
                        : referansKunye.KunyeNo.ToString(CultureInfo.InvariantCulture);

                    aggregated[key] = referansKunye;
                }

                completedSteps += 1;
                if (progressCallback is not null)
                {
                    var percent = totalSteps > 0
                        ? Math.Clamp((int)Math.Round((double)completedSteps / totalSteps * 100d), 1, 99)
                        : 99;
                    var label = totalSteps > 1
                        ? $"{product.Ad} | {FormatSegmentLabel(segment)}"
                        : "HKS sorgusu tamamlanmak uzere";

                    await progressCallback(new HksSearchProgressDto
                    {
                        CompletedSteps = completedSteps,
                        TotalSteps = totalSteps,
                        ProgressPercent = percent,
                        Label = label
                    });
                }
            }
        }

        return new HksReferansKunyeResponseDto
        {
            IslemKodu = SuccessIslemKodu,
            HataKodu = 0,
            ReferansKunyeler = aggregated.Values
                .OrderByDescending(item => item.BildirimTarihi)
                .ThenByDescending(item => item.KunyeNo)
                .ToList(),
        };
    }

    private static string FormatSegmentLabel(HksReferansKunyeRequestDto request)
    {
        var start = request.BaslangicTarihi?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "?";
        var end = request.BitisTarihi?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "?";
        return $"{start} - {end}";
    }

    private static IReadOnlyList<HksReferansKunyeRequestDto> BuildDateSegments(HksReferansKunyeRequestDto request)
    {
        if (!request.BaslangicTarihi.HasValue
            || !request.BitisTarihi.HasValue
            || request.BitisTarihi.Value <= request.BaslangicTarihi.Value.AddMonths(1))
        {
            return [CloneRequest(request)];
        }

        var segments = new List<HksReferansKunyeRequestDto>();
        var currentStart = request.BaslangicTarihi.Value;
        var finalEnd = request.BitisTarihi.Value;

        while (currentStart <= finalEnd)
        {
            var currentEnd = currentStart.AddMonths(1);
            if (currentEnd > finalEnd)
            {
                currentEnd = finalEnd;
            }

            segments.Add(new HksReferansKunyeRequestDto
            {
                MalinSahibiTcKimlikVergiNo = request.MalinSahibiTcKimlikVergiNo,
                KunyeNo = request.KunyeNo,
                KalanMiktariSifirdanBuyukOlanlar = request.KalanMiktariSifirdanBuyukOlanlar,
                BaslangicTarihi = currentStart,
                BitisTarihi = currentEnd,
                KisiSifat = request.KisiSifat,
                UrunId = request.UrunId,
            });

            if (currentEnd >= finalEnd)
            {
                break;
            }

            currentStart = currentEnd.AddSeconds(1);
        }

        return segments;
    }

    private static HksReferansKunyeRequestDto CloneRequest(HksReferansKunyeRequestDto request)
    {
        return new HksReferansKunyeRequestDto
        {
            MalinSahibiTcKimlikVergiNo = request.MalinSahibiTcKimlikVergiNo,
            KunyeNo = request.KunyeNo,
            KalanMiktariSifirdanBuyukOlanlar = request.KalanMiktariSifirdanBuyukOlanlar,
            BaslangicTarihi = request.BaslangicTarihi,
            BitisTarihi = request.BitisTarihi,
            KisiSifat = request.KisiSifat,
            UrunId = request.UrunId,
        };
    }

    private async Task<HksReferansKunyeResponseDto> GetReferansKunyelerByProductAsync(
        HksReferansKunyeRequestDto request,
        HksCredentialSet credentials,
        CancellationToken cancellationToken)
    {

        var soapRequest = BuildBaseRequest(
            "BaseRequestMessageOf_ReferansKunyeIstek",
            BuildReferansKunyeRequest(request),
            credentials);

        var document = await SendSoapRequestAsync(
            _options.BildirimEndpoint,
            "http://www.gtb.gov.tr//WebServices/IBildirimService/BildirimServisReferansKunyeler",
            soapRequest,
            cancellationToken);

        var baseResponse = ParseBaseResponse(document);
        EnsureBusinessSuccess(baseResponse, "HKS referans kunye listesi alinamadi.");

        return new HksReferansKunyeResponseDto
        {
            IslemKodu = baseResponse.IslemKodu,
            HataKodu = GetNullableInt(baseResponse.SonucElement, "HataKodu"),
            Mesaj = GetString(baseResponse.SonucElement, "Mesaj"),
            HataKodlari = baseResponse.HataKodlari.ToList(),
            ReferansKunyeler = ParseReferansKunyeler(baseResponse.SonucElement),
        };
    }

    private async Task<XDocument> SendSoapRequestAsync(
        string? endpoint,
        string soapAction,
        XElement requestBody,
        CancellationToken cancellationToken)
    {
        EnsureConfigured(endpoint);

        var envelope = BuildEnvelope(requestBody);
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.TryAddWithoutValidation("SOAPAction", soapAction);
        request.Content = new StringContent(
            envelope.ToString(SaveOptions.DisableFormatting),
            Encoding.UTF8,
            MediaTypeNames.Text.Xml);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var faultMessage = ParseSoapFault(content);
            throw new HksIntegrationException(
                faultMessage ?? $"HKS istegi basarisiz oldu. HTTP {(int)response.StatusCode}.",
                StatusCodes.Status502BadGateway);
        }

        try
        {
            return XDocument.Parse(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HKS yaniti XML olarak parse edilemedi.");
            throw new HksIntegrationException(
                "HKS yaniti parse edilemedi.",
                StatusCodes.Status502BadGateway);
        }
    }

    private static void EnsureConfigured(string? endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new HksIntegrationException(
                "HKS endpoint tanimi eksik.",
                StatusCodes.Status503ServiceUnavailable);
        }
    }

    private static XDocument BuildEnvelope(XElement body)
    {
        XNamespace soap = SoapEnvelopeNamespace;

        return new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement(
                soap + "Envelope",
                new XAttribute(XNamespace.Xmlns + "soap", soap),
                new XElement(soap + "Body", body)));
    }

    private static XElement BuildBaseRequest(string rootName, XElement requestElement, HksCredentialSet credentials)
    {
        XNamespace service = ServiceNamespace;

        return new XElement(
            service + rootName,
            requestElement,
            new XElement(service + "Password", credentials.Password),
            new XElement(service + "ServicePassword", credentials.ServicePassword),
            new XElement(service + "UserName", credentials.UserName));
    }

    private static XElement BuildEmptyRequest()
    {
        XNamespace service = ServiceNamespace;
        return new XElement(service + "Istek");
    }

    private static XElement BuildSingleIntRequest(string name, int value, string contractNamespace)
    {
        XNamespace service = ServiceNamespace;
        XNamespace contract = contractNamespace;

        var requestElement = new XElement(service + "Istek");
        AddIfHasValue(requestElement, contract, name, value);
        return requestElement;
    }

    private static XElement BuildStringArrayRequest(
        string name,
        IReadOnlyCollection<string> values,
        string contractNamespace)
    {
        XNamespace service = ServiceNamespace;
        XNamespace contract = contractNamespace;
        XNamespace arrays = ArrayNamespace;

        var requestElement = new XElement(service + "Istek");
        var arrayContainer = new XElement(contract + name);

        foreach (var value in values
                     .Select(Normalize)
                     .Where(value => !string.IsNullOrWhiteSpace(value)))
        {
            arrayContainer.Add(new XElement(arrays + "string", value));
        }

        requestElement.Add(arrayContainer);
        return requestElement;
    }

    private static XElement BuildSingleStringRequest(string name, string value, string contractNamespace)
    {
        XNamespace service = ServiceNamespace;
        XNamespace contract = contractNamespace;

        var requestElement = new XElement(service + "Istek");
        AddIfHasValue(requestElement, contract, name, value);
        return requestElement;
    }

    private static XElement BuildReferansKunyeRequest(HksReferansKunyeRequestDto request)
    {
        XNamespace service = ServiceNamespace;
        XNamespace contract = BildirimContractNamespace;

        var requestElement = new XElement(service + "Istek");

        AddIfHasValue(requestElement, contract, "BaslangicTarihi", request.BaslangicTarihi);
        AddIfHasValue(requestElement, contract, "BitisTarihi", request.BitisTarihi);
        AddIfHasValue(requestElement, contract, "KalanMiktariSifirdanBuyukOlanlar", request.KalanMiktariSifirdanBuyukOlanlar);
        AddIfHasValue(requestElement, contract, "KisiSifat", request.KisiSifat);
        AddIfHasValue(requestElement, contract, "KunyeNo", request.KunyeNo);
        AddIfHasValue(requestElement, contract, "MalinSahibiTcKimlikVergiNo", Normalize(request.MalinSahibiTcKimlikVergiNo));
        AddIfHasValue(requestElement, contract, "UrunId", request.UrunId);

        return requestElement;
    }

    private static XElement BuildBildirimKayitRequest(IReadOnlyList<HksBildirimKayitItemDto> items)
    {
        XNamespace service = ServiceNamespace;
        XNamespace contract = BildirimContractNamespace;
        XNamespace model = BildirimModelNamespace;

        var requestElement = new XElement(service + "Istek");
        foreach (var item in items)
        {
            var itemElement = new XElement(contract + "BildirimKayitIstek");
            AddIfHasValue(itemElement, contract, "UniqueId", Normalize(item.UniqueId));
            AddIfHasValue(itemElement, contract, "BildirimTuru", item.BildirimTuru);
            AddIfHasValue(itemElement, contract, "ReferansBildirimKunyeNo", item.ReferansBildirimKunyeNo);

            var bildirimciBilgileri = new XElement(contract + "BildirimciBilgileri");
            AddIfHasValue(bildirimciBilgileri, model, "KisiSifat", item.BildirimciBilgileri.KisiSifat);
            itemElement.Add(bildirimciBilgileri);

            var ikinciKisiBilgileri = new XElement(contract + "IkinciKisiBilgileri");
            AddIfHasValue(ikinciKisiBilgileri, model, "KisiSifat", item.IkinciKisiBilgileri.KisiSifat);
            AddIfHasValue(ikinciKisiBilgileri, model, "TcKimlikVergiNo", Normalize(item.IkinciKisiBilgileri.TcKimlikVergiNo));
            AddIfHasValue(ikinciKisiBilgileri, model, "DogumTarihi", item.IkinciKisiBilgileri.DogumTarihi);
            AddIfHasValue(ikinciKisiBilgileri, model, "AdSoyad", Normalize(item.IkinciKisiBilgileri.AdSoyad));
            AddIfHasValue(ikinciKisiBilgileri, model, "Eposta", Normalize(item.IkinciKisiBilgileri.Eposta));
            AddIfHasValue(ikinciKisiBilgileri, model, "CepTel", Normalize(item.IkinciKisiBilgileri.CepTel));
            AddIfHasValue(ikinciKisiBilgileri, model, "YurtDisiMi", item.IkinciKisiBilgileri.YurtDisiMi);
            itemElement.Add(ikinciKisiBilgileri);

            var malinGidecekYerBilgileri = new XElement(contract + "MalinGidecekYerBilgileri");
            AddIfHasValue(malinGidecekYerBilgileri, model, "GidecekYerIsletmeTuruId", item.MalinGidecekYerBilgileri.GidecekYerIsletmeTuruId);
            AddIfHasValue(malinGidecekYerBilgileri, model, "GidecekIsyeriId", item.MalinGidecekYerBilgileri.GidecekIsyeriId);
            AddIfHasValue(malinGidecekYerBilgileri, model, "GidecekUlkeId", item.MalinGidecekYerBilgileri.GidecekUlkeId);
            AddIfHasValue(malinGidecekYerBilgileri, model, "GidecekYerIlId", item.MalinGidecekYerBilgileri.GidecekYerIlId);
            AddIfHasValue(malinGidecekYerBilgileri, model, "GidecekYerIlceId", item.MalinGidecekYerBilgileri.GidecekYerIlceId);
            AddIfHasValue(malinGidecekYerBilgileri, model, "GidecekYerBeldeId", item.MalinGidecekYerBilgileri.GidecekYerBeldeId);
            AddIfHasValue(malinGidecekYerBilgileri, model, "BelgeNo", Normalize(item.MalinGidecekYerBilgileri.BelgeNo));
            AddIfHasValue(malinGidecekYerBilgileri, model, "BelgeTipi", item.MalinGidecekYerBilgileri.BelgeTipi);
            AddIfHasValue(malinGidecekYerBilgileri, model, "AracPlakaNo", Normalize(item.MalinGidecekYerBilgileri.AracPlakaNo));
            itemElement.Add(malinGidecekYerBilgileri);

            var bildirimMalBilgileri = new XElement(contract + "BildirimMalBilgileri");
            AddIfHasValue(bildirimMalBilgileri, model, "UretimIlId", item.BildirimMalBilgileri.UretimIlId);
            AddIfHasValue(bildirimMalBilgileri, model, "UretimIlceId", item.BildirimMalBilgileri.UretimIlceId);
            AddIfHasValue(bildirimMalBilgileri, model, "UretimBeldeId", item.BildirimMalBilgileri.UretimBeldeId);
            AddIfHasValue(bildirimMalBilgileri, model, "MalinNiteligi", item.BildirimMalBilgileri.MalinNiteligi);
            AddIfHasValue(bildirimMalBilgileri, model, "MalinKodNo", item.BildirimMalBilgileri.MalinKodNo);
            AddIfHasValue(bildirimMalBilgileri, model, "UretimSekli", item.BildirimMalBilgileri.UretimSekli);
            AddIfHasValue(bildirimMalBilgileri, model, "MalinCinsiId", item.BildirimMalBilgileri.MalinCinsiId);
            AddIfHasValue(bildirimMalBilgileri, model, "MiktarBirimId", item.BildirimMalBilgileri.MiktarBirimId);
            AddIfHasValue(bildirimMalBilgileri, model, "MalinMiktari", item.BildirimMalBilgileri.MalinMiktari);
            AddIfHasValue(bildirimMalBilgileri, model, "MalinSatisFiyat", item.BildirimMalBilgileri.MalinSatisFiyat);
            AddIfHasValue(bildirimMalBilgileri, model, "GelenUlkeId", item.BildirimMalBilgileri.GelenUlkeId);
            AddIfHasValue(bildirimMalBilgileri, model, "AnalizeGonderilecekMi", item.BildirimMalBilgileri.AnalizeGonderilecekMi);
            itemElement.Add(bildirimMalBilgileri);

            requestElement.Add(itemElement);
        }

        return requestElement;
    }

    private static void AddIfHasValue(XElement parent, XNamespace valueNamespace, string name, object? value)
    {
        if (value is null)
        {
            return;
        }

        if (value is string stringValue)
        {
            if (string.IsNullOrWhiteSpace(stringValue))
            {
                return;
            }

            parent.Add(new XElement(valueNamespace + name, stringValue.Trim()));
            return;
        }

        if (value is DateTime dateTime)
        {
            parent.Add(
                new XElement(
                    valueNamespace + name,
                    dateTime.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)));
            return;
        }

        if (value is bool booleanValue)
        {
            parent.Add(new XElement(valueNamespace + name, booleanValue ? "true" : "false"));
            return;
        }

        parent.Add(new XElement(valueNamespace + name, Convert.ToString(value, CultureInfo.InvariantCulture)));
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private BaseSoapResponse ParseBaseResponse(XDocument document)
    {
        var islemKodu = GetString(document.Root, "IslemKodu");
        var sonuc = FindDescendant(document.Root, "Sonuc");
        var hataKodlari = document
            .Descendants()
            .Where(element => element.Name.LocalName == "ErrorModel")
            .Select(error => new HksErrorDto
            {
                HataKodu = GetNullableInt(error, "HataKodu") ?? 0,
                Mesaj = GetString(error, "Mesaj"),
            })
            .ToList();

        return new BaseSoapResponse(islemKodu, sonuc, hataKodlari);
    }

    private static void EnsureBusinessSuccess(BaseSoapResponse response, string defaultMessage)
    {
        if (string.Equals(response.IslemKodu, SuccessIslemKodu, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var message =
            response.HataKodlari.FirstOrDefault(error => !string.IsNullOrWhiteSpace(error.Mesaj))?.Mesaj
            ?? GetString(response.SonucElement, "Mesaj")
            ?? defaultMessage;

        throw new HksIntegrationException(
            message,
            StatusCodes.Status502BadGateway,
            response.IslemKodu,
            response.HataKodlari);
    }

    private static List<HksSelectOptionDto> ParseOptions(
        XElement? container,
        string itemElementName,
        params string[] labelCandidates)
    {
        if (container is null)
        {
            return [];
        }

        return container
            .Elements()
            .Where(element => element.Name.LocalName == itemElementName || element.HasElements)
            .Select(item => new HksSelectOptionDto
            {
                Id = GetNullableInt(item, "Id") ?? 0,
                Ad = ResolveOptionLabel(item, labelCandidates),
            })
            .Where(item => item.Id > 0 && !string.IsNullOrWhiteSpace(item.Ad))
            .OrderBy(item => item.Ad, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string ResolveOptionLabel(XElement item, IReadOnlyList<string> labelCandidates)
    {
        foreach (var candidate in labelCandidates)
        {
            var value = GetString(item, candidate);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return item
            .Elements()
            .Select(element => Normalize(element.Value))
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))
            ?? string.Empty;
    }

    private static List<HksUrunCinsiDto> ParseUrunCinsleri(XElement? sonucElement)
    {
        var container = FindDescendant(sonucElement, "UrunCinsleri");
        if (container is null)
        {
            return [];
        }

        return container
            .Elements()
            .Where(element => element.HasElements)
            .Select(item => new HksUrunCinsiDto
            {
                HksUrunCinsiId = GetNullableInt(item, "Id") ?? 0,
                HksUrunId = GetNullableInt(item, "UrunId") ?? 0,
                HksUretimSekliId = GetNullableInt(item, "UretimSekliId"),
                Ad = GetString(item, "UrunCinsiAdi") ?? GetString(item, "Adi") ?? string.Empty,
                UrunKodu = GetString(item, "UrunKodu"),
                IthalMi = GetNullableBool(item, "Ithalmi")
            })
            .Where(item => item.HksUrunCinsiId > 0 && item.HksUrunId > 0 && !string.IsNullOrWhiteSpace(item.Ad))
            .OrderBy(item => item.Ad, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<HksReferansKunyeDto> ParseReferansKunyeler(XElement? sonucElement)
    {
        var container = FindDescendant(sonucElement, "ReferansKunyeler");
        if (container is null)
        {
            return [];
        }

        return container
            .Elements()
            .Where(element => element.HasElements)
            .Select(item => new HksReferansKunyeDto
            {
                KunyeNo = GetNullableLong(item, "KunyeNo") ?? 0,
                BildirimTarihi = GetNullableDateTime(item, "BildirimTarihi"),
                MalinKodNo = GetNullableInt(item, "MalinKodNo"),
                MalinAdi = GetString(item, "MalinAdi"),
                MalinCinsKodNo = GetNullableInt(item, "MalinCinsKodNo"),
                MalinCinsi = GetString(item, "MalinCinsi"),
                MalinTuruKodNo = GetNullableInt(item, "MalinTuruKodNo"),
                MalinTuru = GetString(item, "MalinTuru"),
                MalinMiktari = GetNullableDouble(item, "MalinMiktari"),
                KalanMiktar = GetNullableDouble(item, "KalanMiktar"),
                MiktarBirimId = GetNullableInt(item, "MiktarBirimId"),
                MiktarBirimiAd = GetString(item, "MiktarBirimiAd") ?? GetString(item, "MiktarBirimAd"),
                UretimIlId = GetNullableInt(item, "UretimIlId"),
                UretimIlceId = GetNullableInt(item, "UretimIlceId"),
                UretimBeldeId = GetNullableInt(item, "UretimBeldeId"),
                GidecekYerIsletmeTuruId = GetNullableInt(item, "GidecekYerIsletmeTuruId") ?? GetNullableInt(item, "GidecekYerTuruId"),
                GidecekIsyeriId = GetNullableInt(item, "GidecekIsyeriId"),
                GidecekYerIlId = GetNullableInt(item, "GidecekYerIlId"),
                GidecekYerIlceId = GetNullableInt(item, "GidecekYerIlceId"),
                GidecekYerBeldeId = GetNullableInt(item, "GidecekYerBeldeId"),
                BelgeNo = GetString(item, "BelgeNo"),
                BelgeTipi = GetNullableInt(item, "BelgeTipi"),
                AracPlakaNo = GetString(item, "AracPlakaNo"),
                MalinSahibiTcKimlikVergiNo = GetString(item, "MalinSahibiTcKimlikVergiNo"),
                UreticiTcKimlikVergiNo = GetString(item, "UreticiTcKimlikVergiNo"),
                BildirimciTcKimlikVergiNo = GetString(item, "BildirimciTcKimlikVergiNo"),
                Sifat = GetNullableInt(item, "Sifat"),
                UniqueId = GetString(item, "UniqueId"),
            })
            .Where(item => item.KunyeNo > 0)
            .OrderByDescending(item => item.BildirimTarihi)
            .ThenByDescending(item => item.KunyeNo)
            .ToList();
    }

    private static List<HksKayitliKisiSorguDto> ParseKayitliKisiSorgu(XElement? sonucElement)
    {
        var container = FindDescendant(sonucElement, "TcKimlikVergiNolar");
        if (container is null)
        {
            return [];
        }

        return container
            .Elements()
            .Where(element => element.Name.LocalName == "KayitliKisiSorguDTO" || element.HasElements)
            .Select(item => new HksKayitliKisiSorguDto
            {
                TcKimlikVergiNo = GetString(item, "TcKimlikVergiNo") ?? string.Empty,
                KayitliKisiMi = GetNullableBool(item, "KayitliKisiMi") ?? false,
                SifatIds = ParseIntArray(FindDescendant(item, "Sifatlari")),
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.TcKimlikVergiNo))
            .ToList();
    }

    private static List<HksHalIciIsyeriDto> ParseHalIciIsyerleri(XElement? sonucElement, string? expectedTcKimlikVergiNo)
    {
        var container = FindDescendant(sonucElement, "Isyerleri");
        if (container is null)
        {
            return [];
        }

        return container
            .Elements()
            .Where(element => element.Name.LocalName == "HalIciIsyeriDTO" || element.HasElements)
            .Select(item => new HksHalIciIsyeriDto
            {
                Id = GetNullableInt(item, "Id") ?? 0,
                HalId = GetNullableInt(item, "HalId"),
                HalAdi = GetString(item, "HalAdi"),
                Ad = GetString(item, "IsyeriAdi") ?? string.Empty,
                TcKimlikVergiNo = GetString(item, "TcKimlikVergiNo"),
            })
            .Where(item => item.Id > 0 && !string.IsNullOrWhiteSpace(item.Ad))
            .Where(item => string.IsNullOrWhiteSpace(expectedTcKimlikVergiNo)
                || string.IsNullOrWhiteSpace(item.TcKimlikVergiNo)
                || string.Equals(item.TcKimlikVergiNo, expectedTcKimlikVergiNo, StringComparison.OrdinalIgnoreCase))
            .OrderBy(item => item.HalAdi, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Ad, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<HksBildirimKayitSonucDto> ParseBildirimKayitSonuclari(
        XElement? sonucElement,
        IReadOnlyCollection<HksBildirimKayitItemDto> requestItems)
    {
        if (sonucElement is null)
        {
            return [];
        }

        var detailIdByUniqueId = requestItems
            .Where(item => item.FaturaDetayId.HasValue && !string.IsNullOrWhiteSpace(item.UniqueId))
            .ToDictionary(item => item.UniqueId, item => item.FaturaDetayId!.Value, StringComparer.OrdinalIgnoreCase);

        return sonucElement
            .Elements()
            .Where(element => element.Name.LocalName == "BildirimKayitCevap" || element.HasElements)
            .Select(item =>
            {
                var uniqueId = GetString(item, "UniqueId");
                var result = new HksBildirimKayitSonucDto
                {
                    UniqueId = uniqueId,
                    YeniKunyeNo = GetNullableLong(item, "YeniKunyeNo") ?? 0,
                    KayitTarihi = GetNullableDateTime(item, "KayitTarihi"),
                    MalinKodNo = GetNullableInt(item, "MalinKodNo"),
                    MalinCinsiId = GetNullableInt(item, "MalinCinsiId"),
                    UretimSekli = GetNullableInt(item, "UretimSekli"),
                    UretimIlId = GetNullableInt(item, "UretimIlId"),
                    UretimIlceId = GetNullableInt(item, "UretimIlceId"),
                    UretimBeldeId = GetNullableInt(item, "UretimBeldeId"),
                    UreticisininAdUnvani = GetString(item, "UreticisininAdUnvani"),
                    MalinSahibAdi = GetString(item, "MalinSahibAdi"),
                    MalinMiktari = GetNullableDouble(item, "MalinMiktari"),
                    MiktarBirimId = GetNullableInt(item, "MiktarBirimId"),
                    AracPlakaNo = GetString(item, "AracPlakaNo"),
                    RusumMiktari = GetNullableDouble(item, "RusumMiktari"),
                    BelgeNo = GetString(item, "BelgeNo"),
                    BelgeTipi = GetNullableInt(item, "BelgeTipi"),
                };

                if (!string.IsNullOrWhiteSpace(uniqueId)
                    && detailIdByUniqueId.TryGetValue(uniqueId, out var detailId))
                {
                    result.FaturaDetayId = detailId;
                }

                return result;
            })
            .Where(item => item.YeniKunyeNo > 0)
            .ToList();
    }

    private static List<int> ParseIntArray(XElement? parent)
    {
        if (parent is null)
        {
            return [];
        }

        return parent
            .Elements()
            .Select(element =>
            {
                if (int.TryParse(element.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
                {
                    return value;
                }

                return (int?)null;
            })
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .Distinct()
            .ToList();
    }

    private static string? ParseSoapFault(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return null;
        }

        try
        {
            var document = XDocument.Parse(responseBody);
            return GetString(document.Root, "faultstring")
                ?? GetString(document.Root, "Fault")
                ?? GetString(document.Root, "Reason");
        }
        catch
        {
            return null;
        }
    }

    private static XElement? FindDescendant(XElement? parent, string localName)
    {
        return parent?.Descendants().FirstOrDefault(element => element.Name.LocalName == localName);
    }

    private static string? GetString(XElement? parent, string localName)
    {
        var value = FindDescendant(parent, localName)?.Value;
        return Normalize(value);
    }

    private static int? GetNullableInt(XElement? parent, string localName)
    {
        return int.TryParse(GetString(parent, localName), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }

    private static long? GetNullableLong(XElement? parent, string localName)
    {
        return long.TryParse(GetString(parent, localName), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }

    private static double? GetNullableDouble(XElement? parent, string localName)
    {
        return double.TryParse(GetString(parent, localName), NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }

    private static DateTime? GetNullableDateTime(XElement? parent, string localName)
    {
        return DateTime.TryParse(
            GetString(parent, localName),
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeLocal | DateTimeStyles.AllowWhiteSpaces,
            out var value)
            ? value
            : null;
    }

    private static bool? GetNullableBool(XElement? parent, string localName)
    {
        return bool.TryParse(GetString(parent, localName), out var value)
            ? value
            : null;
    }

    private sealed record BaseSoapResponse(
        string? IslemKodu,
        XElement? SonucElement,
        IReadOnlyList<HksErrorDto> HataKodlari);
}
