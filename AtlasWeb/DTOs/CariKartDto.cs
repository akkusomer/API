using AtlasWeb.Models;

namespace AtlasWeb.DTOs
{
    public class CariKartDto
    {
        public Guid CariTipId { get; set; }
        public string? Unvan { get; set; }
        public string? AdiSoyadi { get; set; }
        public FaturaTipiEnum FaturaTipi { get; set; } = FaturaTipiEnum.Bireysel;
        public string? GrupKodu { get; set; }
        public string? OzelKodu { get; set; }
        public string? Telefon { get; set; }
        public string? Telefon2 { get; set; }
        public string? Gsm { get; set; }
        public string? Adres { get; set; }
        public int? HksIlId { get; set; }
        public int? HksIlceId { get; set; }
        public int? HksBeldeId { get; set; }
        public int? HksSifatId { get; set; }
        public int? HksIsletmeTuruId { get; set; }
        public int? HksHalIciIsyeriId { get; set; }
        public string? Il { get; set; }
        public string? Ilce { get; set; }
        public string? Belde { get; set; }
        public string? Sifat { get; set; }
        public string? IsletmeTuru { get; set; }
        public string? HalIciIsyeriAdi { get; set; }
        public string? VergiDairesi { get; set; }
        public string? VTCK_No { get; set; }
        public DateTime? DogumTarihi { get; set; }
    }
}
