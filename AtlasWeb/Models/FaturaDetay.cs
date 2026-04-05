using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace AtlasWeb.Models
{
    public class FaturaDetay : BaseEntity
    {
        public Guid FaturaId { get; set; }

        [ForeignKey(nameof(FaturaId))]
        public virtual Fatura Fatura { get; set; } = null!;

        public Guid StokId { get; set; }

        [ForeignKey(nameof(StokId))]
        public virtual Stok Stok { get; set; } = null!;

        [MaxLength(120)]
        public string? AlisKunye { get; set; }

        [MaxLength(120)]
        public string? SatisKunye { get; set; }

        public decimal Miktar { get; set; }
        public decimal BirimFiyat { get; set; }
        public decimal SatirToplami { get; set; }
    }
}
