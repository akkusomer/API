namespace AtlasWeb.DTOs
{
    public class StokDto
    {
        public string StokAdi { get; set; } = string.Empty;
        public string? YedekAdi { get; set; }
        public Guid BirimId { get; set; }
    }
}
