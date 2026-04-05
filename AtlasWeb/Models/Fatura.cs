using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AtlasWeb.Models
{
    public class Fatura : BaseEntity
    {
        [Required]
        [MaxLength(30)]
        public string FaturaNo { get; set; } = string.Empty;

        public Guid? CariKartId { get; set; }

        [ForeignKey(nameof(CariKartId))]
        public virtual CariKart? CariKart { get; set; }

        public DateTime FaturaTarihi { get; set; } = DateTime.UtcNow;

        [MaxLength(120)]
        public string? AlisKunye { get; set; }

        [MaxLength(300)]
        public string? Aciklama { get; set; }

        public decimal Tutar { get; set; }
        public decimal TahsilEdilenTutar { get; set; }

        public virtual ICollection<FaturaDetay> Detaylar { get; set; } = new List<FaturaDetay>();
    }
}
