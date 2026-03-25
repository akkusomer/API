using System.ComponentModel.DataAnnotations;

namespace AtlasWeb.Models
{
    public enum KimlikTuruEnum { VKN, TCKN }
    public enum PaketTipiEnum { Standart, Premium, Kurumsal }

    public class Musteri : ISoftDelete
    {
        [Key] // Bu satır Id'nin Primary Key (Birincil Anahtar) olduğunu belirtir
        public Guid Id { get; set; }

        [Required]
        public string MusteriKodu { get; set; } = string.Empty;

        [Required]
        public string Unvan { get; set; } = string.Empty;

        public string VergiNo { get; set; } = string.Empty;
        public string VergiDairesi { get; set; } = string.Empty;
        public KimlikTuruEnum KimlikTuru { get; set; } = KimlikTuruEnum.VKN;

        public string GsmNo { get; set; } = string.Empty;
        public string EPosta { get; set; } = string.Empty;

        public string Il { get; set; } = string.Empty;
        public string Ilce { get; set; } = string.Empty;
        public string AdresDetay { get; set; } = string.Empty;

        public PaketTipiEnum PaketTipi { get; set; } = PaketTipiEnum.Standart;
        public bool AktifMi { get; set; } = true;

        public DateTime KayitTarihi { get; set; } = DateTime.UtcNow;
    }
}