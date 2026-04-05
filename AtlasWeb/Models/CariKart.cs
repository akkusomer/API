using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AtlasWeb.Models
{
    public enum FaturaTipiEnum
    {
        Bireysel = 1,
        Kurumsal = 2,
        Ihracat = 3
    }

    public class CariKart : BaseEntity
    {
        [Required]
        public Guid CariTipId { get; set; }

        [ForeignKey(nameof(CariTipId))]
        public virtual CariTip? CariTip { get; set; }

        [MaxLength(150)]
        public string? Unvan { get; set; }

        [MaxLength(100)]
        public string? AdiSoyadi { get; set; }

        public FaturaTipiEnum FaturaTipi { get; set; } = FaturaTipiEnum.Bireysel;

        [MaxLength(20)]
        public string? GrupKodu { get; set; }

        [MaxLength(20)]
        public string? OzelKodu { get; set; }

        [MaxLength(20)]
        public string? Telefon { get; set; }

        [MaxLength(20)]
        public string? Telefon2 { get; set; }

        [MaxLength(20)]
        public string? Gsm { get; set; }

        [MaxLength(250)]
        public string? Adres { get; set; }

        public int? HksIlId { get; set; }

        public int? HksIlceId { get; set; }

        public int? HksBeldeId { get; set; }

        public int? HksSifatId { get; set; }

        public int? HksIsletmeTuruId { get; set; }

        public int? HksHalIciIsyeriId { get; set; }

        [MaxLength(100)]
        public string? Il { get; set; }

        [MaxLength(100)]
        public string? Ilce { get; set; }

        [MaxLength(100)]
        public string? Belde { get; set; }

        [MaxLength(150)]
        public string? HalIciIsyeriAdi { get; set; }

        [MaxLength(50)]
        public string? VergiDairesi { get; set; }

        [MaxLength(11)]
        public string? VTCK_No { get; set; }

        [Column(TypeName = "date")]
        public DateTime? DogumTarihi { get; set; }
    }
}
