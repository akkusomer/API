using System.ComponentModel.DataAnnotations;

namespace AtlasWeb.Models
{
    public class CariTip : BaseEntity
    {
        [Required, MaxLength(50)]
        public string Adi { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Aciklama { get; set; }

        public virtual ICollection<CariKart> CariKartlar { get; set; } = new List<CariKart>();
    }
}
