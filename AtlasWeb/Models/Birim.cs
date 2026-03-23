using System.ComponentModel.DataAnnotations;
using AtlasWeb.Services;

namespace AtlasWeb.Models
{
    public class Birim : ISoftDelete, IAuditEntity
    {
        [Key]
        public Guid Id { get; set; } = IdGenerator.CreateV7();
        public string Ad { get; set; } = string.Empty;
        public string Sembol { get; set; } = string.Empty;

        // ISoftDelete gereksinimleri
        public bool AktifMi { get; set; } = true;
        public DateTime? SilinmeTarihi { get; set; }
        public string? SilenKullanici { get; set; }

        // IAuditEntity gereksinimleri
        public DateTime KayitTarihi { get; set; } = DateTime.UtcNow;
        public string? OlusturanKullanici { get; set; }
        public DateTime? GuncellemeTarihi { get; set; }
        public string? GuncelleyenKullanici { get; set; }
        public AuditSource Source { get; set; } = AuditSource.Web;
    }
}
