namespace AtlasWeb.Services;

public sealed class HksOptions
{
    public const string SectionName = "Hks";

    public string BildirimEndpoint { get; set; } = "https://ws.gtb.gov.tr:8443/HKSBildirimService";
    public string GenelEndpoint { get; set; } = "https://ws.gtb.gov.tr:8443/HKSGenelService";
    public string UrunEndpoint { get; set; } = "https://ws.gtb.gov.tr:8443/HKSUrunService";
    public int TimeoutSeconds { get; set; } = 60;
}
