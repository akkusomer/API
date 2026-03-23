namespace AtlasWeb.Models
{
    public class ErrorLog
    {
        public int Id { get; set; }
        public string HataMesaji { get; set; } = string.Empty;
        public string? HataDetayi { get; set; }
        public string? IstekYolu { get; set; }
        public string? KullaniciId { get; set; }
        public DateTime Tarih { get; set; } = DateTime.UtcNow;
    }
}
