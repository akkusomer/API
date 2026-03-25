using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AtlasWeb.Models
{
    /// <summary>
    /// Fatura tipi — Bireysel veya kurumsal ayrımı için.
    /// </summary>
    public enum FaturaTipiEnum
    {
        Bireysel = 1,
        Kurumsal = 2,
        Ihracat  = 3
    }

    /// <summary>
    /// Cari kart — müşteri, tedarikçi veya diğer cari hesaplar.
    /// BaseEntity üzerinden kiracı izolasyonu (MusteriId) ve soft-delete desteklenir.
    /// </summary>
    public class CariKart : BaseEntity
    {
        // ── Tip bağlantısı ──────────────────────────────────────────────────
        [Required]
        public Guid CariTipId { get; set; }

        [ForeignKey(nameof(CariTipId))]
        public virtual CariTip? CariTip { get; set; }

        // ── Kimlik Bilgileri ─────────────────────────────────────────────────
        /// <summary>Kurumsal cariler için ticari unvan.</summary>
        [MaxLength(150)]
        public string? Unvan { get; set; }

        /// <summary>Bireysel cariler için ad soyad.</summary>
        [MaxLength(100)]
        public string? AdiSoyadi { get; set; }

        // ── Fatura / Sınıflandırma ───────────────────────────────────────────
        public FaturaTipiEnum FaturaTipi { get; set; } = FaturaTipiEnum.Bireysel;

        [MaxLength(20)]
        public string? GrupKodu { get; set; }

        [MaxLength(20)]
        public string? OzelKodu { get; set; }

        // ── İletişim ─────────────────────────────────────────────────────────
        [MaxLength(20)]
        public string? Telefon { get; set; }

        [MaxLength(20)]
        public string? Telefon2 { get; set; }

        [MaxLength(20)]
        public string? Gsm { get; set; }

        [MaxLength(250)]
        public string? Adres { get; set; }   // Düzeltme: Adress → Adres

        // ── Vergi / TC ───────────────────────────────────────────────────────
        [MaxLength(50)]
        public string? VergiDairesi { get; set; }

        /// <summary>VKN (10 hane) veya TCKN (11 hane).</summary>
        [MaxLength(11)]
        public string? VTCK_No { get; set; }
    }
}
