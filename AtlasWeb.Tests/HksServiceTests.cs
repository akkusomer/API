using System.Net;
using System.Text;
using System.Xml.Linq;
using AtlasWeb.DTOs;
using AtlasWeb.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AtlasWeb.Tests;

public sealed class HksServiceTests
{
    [Fact]
    public async Task GetReferansKunyelerAsync_WhenSoapResponseIsValid_ParsesResult()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(BuildReferansKunyeSoapResponse(), Encoding.UTF8, "text/xml"),
        });

        using var httpClient = new HttpClient(handler);
        var service = CreateService(httpClient, new StubHksAyarService());

        var result = await service.GetReferansKunyelerAsync(new HksReferansKunyeRequestDto
        {
            BaslangicTarihi = new DateTime(2026, 3, 1),
            BitisTarihi = new DateTime(2026, 3, 30),
            KalanMiktariSifirdanBuyukOlanlar = true,
            UrunId = 300,
        });

        Assert.Equal("GTBWSRV0000001", result.IslemKodu);
        Assert.Single(result.ReferansKunyeler);

        var first = result.ReferansKunyeler[0];
        Assert.Equal(123456789L, first.KunyeNo);
        Assert.Equal("DOMATES", first.MalinAdi);
        Assert.Equal("12345678901", first.MalinSahibiTcKimlikVergiNo);
        Assert.Equal("UNIQUE-001", first.UniqueId);
        Assert.Equal(150.5, first.KalanMiktar);
    }

    [Fact]
    public async Task GetUrunlerAsync_WhenCredentialsMissing_ThrowsServiceUnavailable()
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler(_ => throw new InvalidOperationException("Should not call upstream")));
        var service = new HksService(
            httpClient,
            Options.Create(new HksOptions()),
            new StubHksAyarService(new HksIntegrationException("Eksik bilgi", 503)),
            NullLogger<HksService>.Instance);

        var exception = await Assert.ThrowsAsync<HksIntegrationException>(() => service.GetUrunlerAsync());

        Assert.Equal(503, exception.StatusCode);
    }

    [Fact]
    public async Task GetIllerAsync_WhenSoapResponseIsValid_ParsesResult()
    {
        string? soapAction = null;
        var handler = new StubHttpMessageHandler(request =>
        {
            soapAction = request.Headers.TryGetValues("SOAPAction", out var values)
                ? values.FirstOrDefault()
                : null;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(BuildIllerSoapResponse(), Encoding.UTF8, "text/xml"),
            };
        });

        using var httpClient = new HttpClient(handler);
        var service = CreateService(httpClient, new StubHksAyarService());

        var result = await service.GetIllerAsync();

        Assert.Equal("http://www.gtb.gov.tr//WebServices/IGenelService/GenelServisIller", soapAction);
        Assert.Equal(2, result.Count);
        Assert.Contains(result, item => item.Id == 6 && item.Ad == "ANKARA");
        Assert.Contains(result, item => item.Id == 34 && item.Ad == "ISTANBUL");
    }

    [Fact]
    public async Task GetKayitliKisiSorguAsync_WhenSoapResponseIsValid_ParsesRegisteredPersonAndSifatlar()
    {
        string? soapAction = null;
        string? requestBody = null;
        var handler = new StubHttpMessageHandler(request =>
        {
            soapAction = request.Headers.TryGetValues("SOAPAction", out var values)
                ? values.FirstOrDefault()
                : null;
            requestBody = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(BuildKayitliKisiSorguSoapResponse(), Encoding.UTF8, "text/xml"),
            };
        });

        using var httpClient = new HttpClient(handler);
        var service = CreateService(httpClient, new StubHksAyarService());

        var result = await service.GetKayitliKisiSorguAsync("10163499474");

        Assert.Equal("http://www.gtb.gov.tr//WebServices/IBildirimService/BildirimServisKayitliKisiSorgu", soapAction);
        Assert.NotNull(requestBody);
        Assert.Contains("TcKimlikVergiNolar", requestBody);
        Assert.Contains("10163499474", requestBody);
        Assert.NotNull(result);
        Assert.True(result!.KayitliKisiMi);
        Assert.Equal("10163499474", result.TcKimlikVergiNo);
        Assert.Equal([2, 7, 12], result.SifatIds);
    }

    [Fact]
    public async Task GetHalIciIsyerleriAsync_WhenSoapResponseIsValid_ParsesResult()
    {
        string? soapAction = null;
        string? requestBody = null;
        var handler = new StubHttpMessageHandler(request =>
        {
            soapAction = request.Headers.TryGetValues("SOAPAction", out var values)
                ? values.FirstOrDefault()
                : null;
            requestBody = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(BuildHalIciIsyerleriSoapResponse(), Encoding.UTF8, "text/xml"),
            };
        });

        using var httpClient = new HttpClient(handler);
        var service = CreateService(httpClient, new StubHksAyarService());

        var result = await service.GetHalIciIsyerleriAsync("10163499474");

        Assert.Equal("http://www.gtb.gov.tr//WebServices/IGenelService/GenelServisHalIciIsyeri", soapAction);
        Assert.NotNull(requestBody);
        Assert.Contains("TcKimlikVergiNo", requestBody);
        Assert.Contains("10163499474", requestBody);
        Assert.Equal(2, result.Count);
        Assert.Contains(result, item => item.Id == 901 && item.Ad == "Komisyoncu Dukkani" && item.HalId == 6 && item.HalAdi == "ANKARA HALI");
        Assert.Contains(result, item => item.Id == 902 && item.Ad == "Tuccar Dukkani" && item.TcKimlikVergiNo == "10163499474");
    }

    [Fact]
    public async Task GetIlcelerAsync_WhenSoapResponseIsValid_ParsesResult()
    {
        string? soapAction = null;
        string? requestBody = null;
        var handler = new StubHttpMessageHandler(request =>
        {
            soapAction = request.Headers.TryGetValues("SOAPAction", out var values)
                ? values.FirstOrDefault()
                : null;
            requestBody = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(BuildIlcelerSoapResponse(), Encoding.UTF8, "text/xml"),
            };
        });

        using var httpClient = new HttpClient(handler);
        var service = CreateService(httpClient, new StubHksAyarService());

        var result = await service.GetIlcelerAsync(6);

        Assert.Equal("http://www.gtb.gov.tr//WebServices/IGenelService/GenelServisIlceler", soapAction);
        Assert.NotNull(requestBody);
        Assert.Contains("IlId", requestBody);
        Assert.Contains(">6<", requestBody);
        Assert.Equal(2, result.Count);
        Assert.Contains(result, item => item.Id == 61 && item.Ad == "CANKAYA");
        Assert.Contains(result, item => item.Id == 62 && item.Ad == "MAMAK");
    }

    [Fact]
    public async Task GetBeldelerAsync_WhenSoapResponseIsValid_ParsesResult()
    {
        string? soapAction = null;
        string? requestBody = null;
        var handler = new StubHttpMessageHandler(request =>
        {
            soapAction = request.Headers.TryGetValues("SOAPAction", out var values)
                ? values.FirstOrDefault()
                : null;
            requestBody = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(BuildBeldelerSoapResponse(), Encoding.UTF8, "text/xml"),
            };
        });

        using var httpClient = new HttpClient(handler);
        var service = CreateService(httpClient, new StubHksAyarService());

        var result = await service.GetBeldelerAsync(61);

        Assert.Equal("http://www.gtb.gov.tr//WebServices/IGenelService/GenelServisBeldeler", soapAction);
        Assert.NotNull(requestBody);
        Assert.Contains("IlceId", requestBody);
        Assert.Contains(">61<", requestBody);
        Assert.Equal(2, result.Count);
        Assert.Contains(result, item => item.Id == 6101 && item.Ad == "KARSIYAKA");
        Assert.Contains(result, item => item.Id == 6102 && item.Ad == "ORNEK BELDE");
    }

    [Fact]
    public async Task GetUrunBirimleriAsync_WhenSoapResponseIsValid_ParsesResult()
    {
        string? soapAction = null;
        var handler = new StubHttpMessageHandler(request =>
        {
            soapAction = request.Headers.TryGetValues("SOAPAction", out var values)
                ? values.FirstOrDefault()
                : null;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(BuildUrunBirimleriSoapResponse(), Encoding.UTF8, "text/xml"),
            };
        });

        using var httpClient = new HttpClient(handler);
        var service = CreateService(httpClient, new StubHksAyarService());

        var result = await service.GetUrunBirimleriAsync();

        Assert.Equal("http://www.gtb.gov.tr//WebServices/IUrunService/UrunServiceUrunBirimleri", soapAction);
        Assert.Equal(2, result.Count);
        Assert.Contains(result, item => item.Id == 1 && item.Ad == "KILOGRAM");
        Assert.Contains(result, item => item.Id == 2 && item.Ad == "ADET");
    }

    [Fact]
    public async Task GetIsletmeTurleriAsync_WhenSoapResponseIsValid_ParsesResult()
    {
        string? soapAction = null;
        var handler = new StubHttpMessageHandler(request =>
        {
            soapAction = request.Headers.TryGetValues("SOAPAction", out var values)
                ? values.FirstOrDefault()
                : null;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(BuildIsletmeTurleriSoapResponse(), Encoding.UTF8, "text/xml"),
            };
        });

        using var httpClient = new HttpClient(handler);
        var service = CreateService(httpClient, new StubHksAyarService());

        var result = await service.GetIsletmeTurleriAsync();

        Assert.Equal("http://www.gtb.gov.tr//WebServices/IGenelService/GenelServisIsletmeTurleri", soapAction);
        Assert.Equal(2, result.Count);
        Assert.Contains(result, item => item.Id == 1 && item.Ad == "KOMISYONCU HALI");
        Assert.Contains(result, item => item.Id == 2 && item.Ad == "TUCCAR HALI");
    }

    [Fact]
    public async Task GetUretimSekilleriAsync_WhenSoapResponseIsValid_ParsesResult()
    {
        string? soapAction = null;
        var handler = new StubHttpMessageHandler(request =>
        {
            soapAction = request.Headers.TryGetValues("SOAPAction", out var values)
                ? values.FirstOrDefault()
                : null;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(BuildUretimSekilleriSoapResponse(), Encoding.UTF8, "text/xml"),
            };
        });

        using var httpClient = new HttpClient(handler);
        var service = CreateService(httpClient, new StubHksAyarService());

        var result = await service.GetUretimSekilleriAsync();

        Assert.Equal("http://www.gtb.gov.tr//WebServices/IUrunService/UrunServiceUretimSekilleri", soapAction);
        Assert.Equal(2, result.Count);
        Assert.Contains(result, item => item.Id == 10 && item.Ad == "GELENEKSEL");
        Assert.Contains(result, item => item.Id == 11 && item.Ad == "ORGANIK");
    }

    [Fact]
    public async Task GetUrunCinsleriAsync_WhenSoapResponseIsValid_ParsesResult()
    {
        string? soapAction = null;
        string? requestBody = null;
        var handler = new StubHttpMessageHandler(request =>
        {
            soapAction = request.Headers.TryGetValues("SOAPAction", out var values)
                ? values.FirstOrDefault()
                : null;
            requestBody = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(BuildUrunCinsleriSoapResponse(), Encoding.UTF8, "text/xml"),
            };
        });

        using var httpClient = new HttpClient(handler);
        var service = CreateService(httpClient, new StubHksAyarService());

        var result = await service.GetUrunCinsleriAsync(300);

        Assert.Equal("http://www.gtb.gov.tr//WebServices/IUrunService/UrunServiceUrunCinsleri", soapAction);
        Assert.NotNull(requestBody);
        Assert.Contains("UrunId", requestBody);
        Assert.Contains(">300<", requestBody);
        Assert.Equal(2, result.Count);
        Assert.Contains(result, item => item.HksUrunCinsiId == 1001 && item.HksUrunId == 300 && item.HksUretimSekliId == 10 && item.Ad == "SERA");
        Assert.Contains(result, item => item.HksUrunCinsiId == 1002 && item.HksUrunId == 300 && item.IthalMi == true && item.UrunKodu == "DMT-ITH");
    }

    [Fact]
    public async Task GetReferansKunyelerAsync_WhenOwnerNumberMissing_UsesStoredHksUserName()
    {
        string? requestBody = null;
        var handler = new StubHttpMessageHandler(request =>
        {
            requestBody = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(BuildReferansKunyeSoapResponse(), Encoding.UTF8, "text/xml"),
            };
        });

        using var httpClient = new HttpClient(handler);
        var service = CreateService(httpClient, new StubHksAyarService());

        await service.GetReferansKunyelerAsync(new HksReferansKunyeRequestDto
        {
            BaslangicTarihi = new DateTime(2026, 3, 1),
            BitisTarihi = new DateTime(2026, 3, 30),
            KalanMiktariSifirdanBuyukOlanlar = true,
            UrunId = 300,
        });

        Assert.NotNull(requestBody);
        Assert.Contains("MalinSahibiTcKimlikVergiNo", requestBody);
        Assert.Contains("hks-user", requestBody);
    }

    [Fact]
    public async Task GetReferansKunyelerAsync_WhenUrunIdMissing_QueriesAllProductsAndAggregatesResults()
    {
        var requests = new List<string>();
        var handler = new StubHttpMessageHandler(request =>
        {
            var body = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult() ?? string.Empty;
            requests.Add(body);
            var soapAction = request.Headers.TryGetValues("SOAPAction", out var values)
                ? values.FirstOrDefault() ?? string.Empty
                : string.Empty;
            var urunId = XDocument.Parse(body)
                .Descendants()
                .FirstOrDefault(element => element.Name.LocalName == "UrunId")
                ?.Value;

            if (soapAction.Contains("UrunServiceUrunler", StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(BuildUrunlerSoapResponse(), Encoding.UTF8, "text/xml"),
                };
            }

            if (soapAction.Contains("BildirimServisReferansKunyeler", StringComparison.OrdinalIgnoreCase)
                && urunId == "300")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(BuildReferansKunyeSoapResponse(123456789L, "DOMATES", "UNIQUE-001"), Encoding.UTF8, "text/xml"),
                };
            }

            if (soapAction.Contains("BildirimServisReferansKunyeler", StringComparison.OrdinalIgnoreCase)
                && urunId == "301")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(BuildReferansKunyeSoapResponse(223456789L, "ACUR", "UNIQUE-002"), Encoding.UTF8, "text/xml"),
                };
            }

            throw new InvalidOperationException("Beklenmeyen HKS istegi.");
        });

        using var httpClient = new HttpClient(handler);
        var service = CreateService(httpClient, new StubHksAyarService());

        var result = await service.GetReferansKunyelerAsync(new HksReferansKunyeRequestDto
        {
            BaslangicTarihi = new DateTime(2026, 3, 1),
            BitisTarihi = new DateTime(2026, 3, 30),
            KalanMiktariSifirdanBuyukOlanlar = true,
        });

        Assert.Equal("GTBWSRV0000001", result.IslemKodu);
        Assert.Equal(2, result.ReferansKunyeler.Count);
        Assert.Contains(result.ReferansKunyeler, item => item.UniqueId == "UNIQUE-001");
        Assert.Contains(result.ReferansKunyeler, item => item.UniqueId == "UNIQUE-002");
        Assert.Equal(3, requests.Count);
    }

    [Fact]
    public async Task GetReferansKunyelerAsync_WhenDateRangeExceedsOneMonth_SplitsRequestIntoMonthlyChunks()
    {
        var requests = new List<string>();
        var handler = new StubHttpMessageHandler(request =>
        {
            var body = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult() ?? string.Empty;
            requests.Add(body);

            if (body.Contains("2026-01-01T00:00:00") && body.Contains("2026-02-01T00:00:00"))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(BuildReferansKunyeSoapResponse(323456789L, "DOMATES", "UNIQUE-003"), Encoding.UTF8, "text/xml"),
                };
            }

            if (body.Contains("2026-02-01T00:00:01") && body.Contains("2026-03-01T00:00:01"))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(BuildReferansKunyeSoapResponse(423456789L, "BIBER", "UNIQUE-004"), Encoding.UTF8, "text/xml"),
                };
            }

            if (body.Contains("2026-03-01T00:00:02") && body.Contains("2026-03-15T00:00:00"))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(BuildReferansKunyeSoapResponse(523456789L, "PATLICAN", "UNIQUE-005"), Encoding.UTF8, "text/xml"),
                };
            }

            throw new InvalidOperationException("Beklenmeyen HKS tarih parcasi.");
        });

        using var httpClient = new HttpClient(handler);
        var service = CreateService(httpClient, new StubHksAyarService());

        var result = await service.GetReferansKunyelerAsync(new HksReferansKunyeRequestDto
        {
            BaslangicTarihi = new DateTime(2026, 1, 1, 0, 0, 0),
            BitisTarihi = new DateTime(2026, 3, 15, 0, 0, 0),
            KalanMiktariSifirdanBuyukOlanlar = true,
            UrunId = 300,
        });

        Assert.Equal("GTBWSRV0000001", result.IslemKodu);
        Assert.Equal(3, result.ReferansKunyeler.Count);
        Assert.Equal(3, requests.Count);
    }

    private static HksService CreateService(HttpClient httpClient, IHksAyarService hksAyarService)
    {
        return new HksService(
            httpClient,
            Options.Create(new HksOptions
            {
                BildirimEndpoint = "https://ws.gtb.gov.tr:8443/HKSBildirimService",
                GenelEndpoint = "https://ws.gtb.gov.tr:8443/HKSGenelService",
                UrunEndpoint = "https://ws.gtb.gov.tr:8443/HKSUrunService",
            }),
            hksAyarService,
            NullLogger<HksService>.Instance);
    }

    private static string BuildReferansKunyeSoapResponse(long kunyeNo = 123456789L, string malinAdi = "DOMATES", string uniqueId = "UNIQUE-001")
    {
        return $"""
            <s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/">
              <s:Body>
                <BaseResponseMessageOf_ReferansKunyeCevap xmlns="http://www.gtb.gov.tr//WebServices">
                  <HataKodlari xmlns:a="http://schemas.datacontract.org/2004/07/GTB.HKS.Core.ServiceContract" />
                  <IslemKodu>GTBWSRV0000001</IslemKodu>
                  <Sonuc xmlns:b="http://schemas.datacontract.org/2004/07/GTB.HKS.Bildirim.ServiceContract">
                    <b:HataKodu>0</b:HataKodu>
                    <b:Mesaj>Basarili</b:Mesaj>
                    <b:ReferansKunyeler xmlns:c="http://schemas.datacontract.org/2004/07/GTB.HKS.Bildirim.Model">
                      <c:ReferansKunyeDTO>
                        <c:KunyeNo>{kunyeNo}</c:KunyeNo>
                        <c:BildirimTarihi>2026-03-29T08:30:00</c:BildirimTarihi>
                        <c:MalinKodNo>101</c:MalinKodNo>
                        <c:MalinAdi>{malinAdi}</c:MalinAdi>
                        <c:MalinCinsKodNo>11</c:MalinCinsKodNo>
                        <c:MalinCinsi>SERA</c:MalinCinsi>
                        <c:MalinTuruKodNo>9</c:MalinTuruKodNo>
                        <c:MalinTuru>KIRMIZI</c:MalinTuru>
                        <c:MalinMiktari>200.5</c:MalinMiktari>
                        <c:KalanMiktar>150.5</c:KalanMiktar>
                        <c:MiktarBirimId>1</c:MiktarBirimId>
                        <c:MiktarBirimiAd>KG</c:MiktarBirimiAd>
                        <c:MalinSahibiTcKimlikVergiNo>12345678901</c:MalinSahibiTcKimlikVergiNo>
                        <c:UreticiTcKimlikVergiNo>10987654321</c:UreticiTcKimlikVergiNo>
                        <c:BildirimciTcKimlikVergiNo>55555555555</c:BildirimciTcKimlikVergiNo>
                        <c:Sifat>2</c:Sifat>
                        <c:UniqueId>{uniqueId}</c:UniqueId>
                      </c:ReferansKunyeDTO>
                    </b:ReferansKunyeler>
                  </Sonuc>
                </BaseResponseMessageOf_ReferansKunyeCevap>
              </s:Body>
            </s:Envelope>
            """;
    }

    private static string BuildUrunlerSoapResponse()
    {
        return """
            <s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/">
              <s:Body>
                <BaseResponseMessageOf_UrunlerCevap xmlns="http://www.gtb.gov.tr//WebServices">
                  <HataKodlari xmlns:a="http://schemas.datacontract.org/2004/07/GTB.HKS.Core.ServiceContract" />
                  <IslemKodu>GTBWSRV0000001</IslemKodu>
                  <Sonuc xmlns:b="http://schemas.datacontract.org/2004/07/GTB.HKS.Urun.ServiceContract">
                    <b:Urunler xmlns:c="http://schemas.datacontract.org/2004/07/GTB.HKS.Urun.Model">
                      <c:UrunDTO>
                        <c:Id>300</c:Id>
                        <c:UrunAdi>DOMATES</c:UrunAdi>
                      </c:UrunDTO>
                      <c:UrunDTO>
                        <c:Id>301</c:Id>
                        <c:UrunAdi>ACUR</c:UrunAdi>
                      </c:UrunDTO>
                    </b:Urunler>
                  </Sonuc>
                </BaseResponseMessageOf_UrunlerCevap>
              </s:Body>
            </s:Envelope>
            """;
    }

    private static string BuildIllerSoapResponse()
    {
        return """
            <s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/">
              <s:Body>
                <BaseResponseMessageOf_IllerCevap xmlns="http://www.gtb.gov.tr//WebServices">
                  <HataKodlari xmlns:a="http://schemas.datacontract.org/2004/07/GTB.HKS.Core.ServiceContract" />
                  <IslemKodu>GTBWSRV0000001</IslemKodu>
                  <Sonuc xmlns:b="http://schemas.datacontract.org/2004/07/GTB.HKS.Genel.ServiceContract">
                    <b:HataKodu>0</b:HataKodu>
                    <b:Mesaj>Basarili</b:Mesaj>
                    <b:Iller xmlns:c="http://schemas.datacontract.org/2004/07/GTB.HKS.Genel.Model">
                      <c:IlDTO>
                        <c:Id>6</c:Id>
                        <c:IlAdi>ANKARA</c:IlAdi>
                      </c:IlDTO>
                      <c:IlDTO>
                        <c:Id>34</c:Id>
                        <c:IlAdi>ISTANBUL</c:IlAdi>
                      </c:IlDTO>
                    </b:Iller>
                  </Sonuc>
                </BaseResponseMessageOf_IllerCevap>
              </s:Body>
            </s:Envelope>
            """;
    }

    private static string BuildKayitliKisiSorguSoapResponse()
    {
        return """
            <s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/">
              <s:Body>
                <BaseResponseMessageOf_KayitliKisiSorguCevap xmlns="http://www.gtb.gov.tr//WebServices">
                  <HataKodlari xmlns:a="http://schemas.datacontract.org/2004/07/GTB.HKS.Core.ServiceContract" />
                  <IslemKodu>GTBWSRV0000001</IslemKodu>
                  <Sonuc xmlns:b="http://schemas.datacontract.org/2004/07/GTB.HKS.Bildirim.ServiceContract">
                    <b:HataKodu>0</b:HataKodu>
                    <b:Mesaj>Basarili</b:Mesaj>
                    <b:TcKimlikVergiNolar xmlns:c="http://schemas.datacontract.org/2004/07/GTB.HKS.Bildirim.Model">
                      <c:KayitliKisiSorguDTO>
                        <c:TcKimlikVergiNo>10163499474</c:TcKimlikVergiNo>
                        <c:KayitliKisiMi>true</c:KayitliKisiMi>
                        <c:Sifatlari xmlns:d="http://schemas.microsoft.com/2003/10/Serialization/Arrays">
                          <d:int>2</d:int>
                          <d:int>7</d:int>
                          <d:int>12</d:int>
                        </c:Sifatlari>
                      </c:KayitliKisiSorguDTO>
                    </b:TcKimlikVergiNolar>
                  </Sonuc>
                </BaseResponseMessageOf_KayitliKisiSorguCevap>
              </s:Body>
            </s:Envelope>
            """;
    }

    private static string BuildHalIciIsyerleriSoapResponse()
    {
        return """
            <s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/">
              <s:Body>
                <BaseResponseMessageOf_HalIciIsyeriCevap xmlns="http://www.gtb.gov.tr//WebServices">
                  <HataKodlari xmlns:a="http://schemas.datacontract.org/2004/07/GTB.HKS.Core.ServiceContract" />
                  <IslemKodu>GTBWSRV0000001</IslemKodu>
                  <Sonuc xmlns:b="http://schemas.datacontract.org/2004/07/GTB.HKS.Genel.ServiceContract">
                    <b:Isyerleri xmlns:c="http://schemas.datacontract.org/2004/07/GTB.HKS.Genel.Model">
                      <c:HalIciIsyeriDTO>
                        <c:Id>901</c:Id>
                        <c:HalId>6</c:HalId>
                        <c:HalAdi>ANKARA HALI</c:HalAdi>
                        <c:IsyeriAdi>Komisyoncu Dukkani</c:IsyeriAdi>
                        <c:TcKimlikVergiNo>10163499474</c:TcKimlikVergiNo>
                      </c:HalIciIsyeriDTO>
                      <c:HalIciIsyeriDTO>
                        <c:Id>902</c:Id>
                        <c:HalId>6</c:HalId>
                        <c:HalAdi>ANKARA HALI</c:HalAdi>
                        <c:IsyeriAdi>Tuccar Dukkani</c:IsyeriAdi>
                        <c:TcKimlikVergiNo>10163499474</c:TcKimlikVergiNo>
                      </c:HalIciIsyeriDTO>
                    </b:Isyerleri>
                  </Sonuc>
                </BaseResponseMessageOf_HalIciIsyeriCevap>
              </s:Body>
            </s:Envelope>
            """;
    }

    private static string BuildIlcelerSoapResponse()
    {
        return """
            <s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/">
              <s:Body>
                <BaseResponseMessageOf_IlcelerCevap xmlns="http://www.gtb.gov.tr//WebServices">
                  <HataKodlari xmlns:a="http://schemas.datacontract.org/2004/07/GTB.HKS.Core.ServiceContract" />
                  <IslemKodu>GTBWSRV0000001</IslemKodu>
                  <Sonuc xmlns:b="http://schemas.datacontract.org/2004/07/GTB.HKS.Genel.ServiceContract">
                    <b:HataKodu>0</b:HataKodu>
                    <b:Mesaj>Basarili</b:Mesaj>
                    <b:Ilceler xmlns:c="http://schemas.datacontract.org/2004/07/GTB.HKS.Genel.Model">
                      <c:IlceDTO>
                        <c:Id>61</c:Id>
                        <c:IlceAdi>CANKAYA</c:IlceAdi>
                      </c:IlceDTO>
                      <c:IlceDTO>
                        <c:Id>62</c:Id>
                        <c:IlceAdi>MAMAK</c:IlceAdi>
                      </c:IlceDTO>
                    </b:Ilceler>
                  </Sonuc>
                </BaseResponseMessageOf_IlcelerCevap>
              </s:Body>
            </s:Envelope>
            """;
    }

    private static string BuildBeldelerSoapResponse()
    {
        return """
            <s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/">
              <s:Body>
                <BaseResponseMessageOf_BeldelerCevap xmlns="http://www.gtb.gov.tr//WebServices">
                  <HataKodlari xmlns:a="http://schemas.datacontract.org/2004/07/GTB.HKS.Core.ServiceContract" />
                  <IslemKodu>GTBWSRV0000001</IslemKodu>
                  <Sonuc xmlns:b="http://schemas.datacontract.org/2004/07/GTB.HKS.Genel.ServiceContract">
                    <b:HataKodu>0</b:HataKodu>
                    <b:Mesaj>Basarili</b:Mesaj>
                    <b:Beldeler xmlns:c="http://schemas.datacontract.org/2004/07/GTB.HKS.Genel.Model">
                      <c:BeldeDTO>
                        <c:BeldeAdi>KARSIYAKA</c:BeldeAdi>
                        <c:Id>6101</c:Id>
                      </c:BeldeDTO>
                      <c:BeldeDTO>
                        <c:BeldeAdi>ORNEK BELDE</c:BeldeAdi>
                        <c:Id>6102</c:Id>
                      </c:BeldeDTO>
                    </b:Beldeler>
                  </Sonuc>
                </BaseResponseMessageOf_BeldelerCevap>
              </s:Body>
            </s:Envelope>
            """;
    }

    private static string BuildUrunBirimleriSoapResponse()
    {
        return """
            <s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/">
              <s:Body>
                <BaseResponseMessageOf_UrunBirimleriCevap xmlns="http://www.gtb.gov.tr//WebServices">
                  <HataKodlari xmlns:a="http://schemas.datacontract.org/2004/07/GTB.HKS.Core.ServiceContract" />
                  <IslemKodu>GTBWSRV0000001</IslemKodu>
                  <Sonuc xmlns:b="http://schemas.datacontract.org/2004/07/GTB.HKS.Urun.ServiceContract">
                    <b:UrunBirimleri xmlns:c="http://schemas.datacontract.org/2004/07/GTB.HKS.Urun.Model">
                      <c:UrunBirimiDTO>
                        <c:Id>1</c:Id>
                        <c:UrunBirimAdi>KILOGRAM</c:UrunBirimAdi>
                      </c:UrunBirimiDTO>
                      <c:UrunBirimiDTO>
                        <c:Id>2</c:Id>
                        <c:UrunBirimAdi>ADET</c:UrunBirimAdi>
                      </c:UrunBirimiDTO>
                    </b:UrunBirimleri>
                  </Sonuc>
                </BaseResponseMessageOf_UrunBirimleriCevap>
              </s:Body>
            </s:Envelope>
            """;
    }

    private static string BuildIsletmeTurleriSoapResponse()
    {
        return """
            <s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/">
              <s:Body>
                <BaseResponseMessageOf_IsletmeTurleriCevap xmlns="http://www.gtb.gov.tr//WebServices">
                  <HataKodlari xmlns:a="http://schemas.datacontract.org/2004/07/GTB.HKS.Core.ServiceContract" />
                  <IslemKodu>GTBWSRV0000001</IslemKodu>
                  <Sonuc xmlns:b="http://schemas.datacontract.org/2004/07/GTB.HKS.Genel.ServiceContract">
                    <b:IsletmeTurleri xmlns:c="http://schemas.datacontract.org/2004/07/GTB.HKS.Genel.Model">
                      <c:IsletmeTuruDTO>
                        <c:Id>1</c:Id>
                        <c:IsletmeTuruAdi>KOMISYONCU HALI</c:IsletmeTuruAdi>
                      </c:IsletmeTuruDTO>
                      <c:IsletmeTuruDTO>
                        <c:Id>2</c:Id>
                        <c:IsletmeTuruAdi>TUCCAR HALI</c:IsletmeTuruAdi>
                      </c:IsletmeTuruDTO>
                    </b:IsletmeTurleri>
                  </Sonuc>
                </BaseResponseMessageOf_IsletmeTurleriCevap>
              </s:Body>
            </s:Envelope>
            """;
    }

    private static string BuildUretimSekilleriSoapResponse()
    {
        return """
            <s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/">
              <s:Body>
                <BaseResponseMessageOf_UretimSekilleriCevap xmlns="http://www.gtb.gov.tr//WebServices">
                  <HataKodlari xmlns:a="http://schemas.datacontract.org/2004/07/GTB.HKS.Core.ServiceContract" />
                  <IslemKodu>GTBWSRV0000001</IslemKodu>
                  <Sonuc xmlns:b="http://schemas.datacontract.org/2004/07/GTB.HKS.Urun.ServiceContract">
                    <b:UretimSekilleri xmlns:c="http://schemas.datacontract.org/2004/07/GTB.HKS.Urun.Model">
                      <c:UretimSekliDTO>
                        <c:Id>10</c:Id>
                        <c:UretimSekliAdi>GELENEKSEL</c:UretimSekliAdi>
                      </c:UretimSekliDTO>
                      <c:UretimSekliDTO>
                        <c:Id>11</c:Id>
                        <c:UretimSekliAdi>ORGANIK</c:UretimSekliAdi>
                      </c:UretimSekliDTO>
                    </b:UretimSekilleri>
                  </Sonuc>
                </BaseResponseMessageOf_UretimSekilleriCevap>
              </s:Body>
            </s:Envelope>
            """;
    }

    private static string BuildUrunCinsleriSoapResponse()
    {
        return """
            <s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/">
              <s:Body>
                <BaseResponseMessageOf_UrunCinsleriCevap xmlns="http://www.gtb.gov.tr//WebServices">
                  <HataKodlari xmlns:a="http://schemas.datacontract.org/2004/07/GTB.HKS.Core.ServiceContract" />
                  <IslemKodu>GTBWSRV0000001</IslemKodu>
                  <Sonuc xmlns:b="http://schemas.datacontract.org/2004/07/GTB.HKS.Urun.ServiceContract">
                    <b:UrunCinsleri xmlns:c="http://schemas.datacontract.org/2004/07/GTB.HKS.Urun.Model">
                      <c:UrunCinsiDTO>
                        <c:Id>1001</c:Id>
                        <c:UrunCinsiAdi>SERA</c:UrunCinsiAdi>
                        <c:UretimSekliId>10</c:UretimSekliId>
                        <c:UrunId>300</c:UrunId>
                        <c:UrunKodu>DMT-SR</c:UrunKodu>
                        <c:Ithalmi>false</c:Ithalmi>
                      </c:UrunCinsiDTO>
                      <c:UrunCinsiDTO>
                        <c:Id>1002</c:Id>
                        <c:UrunCinsiAdi>ITHAL</c:UrunCinsiAdi>
                        <c:UretimSekliId>11</c:UretimSekliId>
                        <c:UrunId>300</c:UrunId>
                        <c:UrunKodu>DMT-ITH</c:UrunKodu>
                        <c:Ithalmi>true</c:Ithalmi>
                      </c:UrunCinsiDTO>
                    </b:UrunCinsleri>
                  </Sonuc>
                </BaseResponseMessageOf_UrunCinsleriCevap>
              </s:Body>
            </s:Envelope>
            """;
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_responseFactory(request));
        }
    }

    private sealed class StubHksAyarService : IHksAyarService
    {
        private readonly Exception? _exception;

        public StubHksAyarService(Exception? exception = null)
        {
            _exception = exception;
        }

        public Task<HksAyarDto> GetCurrentTenantSettingsAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<HksAyarDto> GetTenantSettingsAsync(Guid tenantId, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<HksCredentialSet> GetCurrentTenantCredentialsAsync(CancellationToken cancellationToken = default)
        {
            if (_exception is not null)
            {
                return Task.FromException<HksCredentialSet>(_exception);
            }

            return Task.FromResult(new HksCredentialSet
            {
                UserName = "hks-user",
                Password = "hks-pass",
                ServicePassword = "service-pass",
            });
        }

        public Task<HksCredentialSet> GetTenantCredentialsAsync(Guid tenantId, CancellationToken cancellationToken = default)
        {
            return GetCurrentTenantCredentialsAsync(cancellationToken);
        }

        public Task<HksAyarDto> SaveCurrentTenantSettingsAsync(HksAyarKaydetDto request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
