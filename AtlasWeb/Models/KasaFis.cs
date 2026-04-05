using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AtlasWeb.Models
{
    public enum KasaIslemTipi
    {
        Tahsilat = 1,
        Odeme = 2
    }

    public class KasaFis : BaseEntity
    {
        [Required]
        [MaxLength(100)]
        public string KasaAdi { get; set; } = "MERKEZ TL KASA";

        [Required]
        [MaxLength(10)]
        public string BelgeKodu { get; set; } = "KF";

        public int BelgeNo { get; set; }

        public KasaIslemTipi IslemTipi { get; set; } = KasaIslemTipi.Tahsilat;

        public Guid? CariKartId { get; set; }

        [ForeignKey(nameof(CariKartId))]
        public virtual CariKart? CariKart { get; set; }

        public DateTime Tarih { get; set; } = DateTime.UtcNow;

        [MaxLength(50)]
        public string? OzelKodu { get; set; }

        [Required]
        [MaxLength(50)]
        public string HareketTipi { get; set; } = "GENEL";

        [MaxLength(200)]
        public string? Aciklama1 { get; set; }

        [MaxLength(200)]
        public string? Aciklama2 { get; set; }

        [MaxLength(50)]
        public string? Pos { get; set; }

        public decimal Tutar { get; set; }
    }
}
