using AtlasWeb.Models;

namespace AtlasWeb.DTOs
{
    public class KasaFisDto
    {
        public string? KasaAdi { get; set; }
        public string? BelgeKodu { get; set; }
        public KasaIslemTipi IslemTipi { get; set; } = KasaIslemTipi.Tahsilat;
        public Guid? CariKartId { get; set; }
        public DateTime Tarih { get; set; } = DateTime.UtcNow;
        public string? OzelKodu { get; set; }
        public string? HareketTipi { get; set; }
        public string? Aciklama1 { get; set; }
        public string? Aciklama2 { get; set; }
        public string? Pos { get; set; }
        public decimal Tutar { get; set; }
    }
}
