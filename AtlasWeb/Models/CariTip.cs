using System.ComponentModel.DataAnnotations;
using AtlasWeb.Services;

namespace AtlasWeb.Models
{
    /// <summary>
    /// Cari kart tipi — Global tanım tablosu (tenant'sız).
    /// Örnek: Müşteri, Tedarikçi, Çalışan, Acente vb.
    /// </summary>
    public class CariTip : ISoftDelete, IAuditEntity
    {
        [Key]
        public Guid Id { get; set; } = IdGenerator.CreateV7();

        [Required, MaxLength(50)]
        public string Adi { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Aciklama { get; set; }

        // ISoftDelete
        public bool AktifMi { get; set; } = true;
        public DateTime? SilinmeTarihi { get; set; }
        public string? SilenKullanici { get; set; }

        // IAuditEntity
        public DateTime KayitTarihi { get; set; } = DateTime.UtcNow;
        public string? OlusturanKullanici { get; set; }
        public DateTime? GuncellemeTarihi { get; set; }
        public string? GuncelleyenKullanici { get; set; }
        public AuditSource Source { get; set; } = AuditSource.Web;

        // Navigation — bir CariTip'e bağlı cari kartlar
        public virtual ICollection<CariKart> CariKartlar { get; set; } = new List<CariKart>();
    }
}
