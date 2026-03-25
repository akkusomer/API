using AtlasWeb.Models;
using FluentValidation;

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
        public string? VergiDairesi { get; set; }
        public string? VTCK_No { get; set; }
    }
}
