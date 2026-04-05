using AtlasWeb.DTOs;

namespace AtlasWeb.Services;

public sealed class HksIntegrationException : Exception
{
    public HksIntegrationException(
        string message,
        int statusCode,
        string? islemKodu = null,
        IReadOnlyList<HksErrorDto>? hataKodlari = null)
        : base(message)
    {
        StatusCode = statusCode;
        IslemKodu = islemKodu;
        HataKodlari = hataKodlari ?? Array.Empty<HksErrorDto>();
    }

    public int StatusCode { get; }

    public string? IslemKodu { get; }

    public IReadOnlyList<HksErrorDto> HataKodlari { get; }
}
