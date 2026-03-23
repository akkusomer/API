using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AtlasWeb.Models
{
    public class KullaniciToken
    {
        [Key] public Guid Id { get; set; } = Guid.NewGuid();
        [Required] public Guid KullaniciId { get; set; }
        [ForeignKey("KullaniciId")] public Kullanici? Kullanici { get; set; }
        [Required] public string RefreshTokenHash { get; set; } = string.Empty;
        public DateTime ExpiryTime { get; set; }
        public string? DeviceInfo { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}