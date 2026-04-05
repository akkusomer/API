using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AtlasWeb.Models
{
    public class KullaniciSifreSifirlamaToken
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid KullaniciId { get; set; }

        [ForeignKey(nameof(KullaniciId))]
        public Kullanici? Kullanici { get; set; }

        [Required]
        public string TokenHash { get; set; } = string.Empty;

        public DateTime ExpiryTime { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ConsumedAt { get; set; }
        public string? RequestedIpAddress { get; set; }
    }
}
