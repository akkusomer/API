using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AtlasWeb.Models
{
    public class Stok : BaseEntity
    {
        public string StokKodu { get; set; } = string.Empty;
        public string StokAdi { get; set; } = string.Empty;
        public string? YedekAdi { get; set; }
        
        public Guid BirimId { get; set; }
        
        [ForeignKey(nameof(BirimId))]
        public virtual Birim Birim { get; set; } = null!;
    }
}
