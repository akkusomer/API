namespace AtlasWeb.DTOs
{
    public class StokDto
    {
        public string StokAdi { get; set; } = string.Empty;
        public string? YedekAdi { get; set; }
        public int? HksUrunId { get; set; }
        public int? HksUretimSekliId { get; set; }
        public int? HksUrunCinsiId { get; set; }
        public string? HksNitelik { get; set; }
        public Guid BirimId { get; set; }
    }
}
