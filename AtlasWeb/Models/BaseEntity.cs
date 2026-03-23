using System.ComponentModel.DataAnnotations;
using AtlasWeb.Services;

namespace AtlasWeb.Models
{
    public abstract class BaseEntity : ITenantEntity, ISoftDelete, IAuditEntity
    {
        [Key]
        public Guid Id { get; set; } = IdGenerator.CreateV7();

        public Guid MusteriId { get; set; }
        public bool AktifMi { get; set; } = true;
        public DateTime KayitTarihi { get; set; } = DateTime.UtcNow;

        // 🛡️ Bu alanlar artik Base'de, artik hata vermez:
        public string? OlusturanKullanici { get; set; }
        public DateTime? GuncellemeTarihi { get; set; }
        public string? GuncelleyenKullanici { get; set; }
        public DateTime? SilinmeTarihi { get; set; }
        public string? SilenKullanici { get; set; }
        public AuditSource Source { get; set; } = AuditSource.Web;
    }
}