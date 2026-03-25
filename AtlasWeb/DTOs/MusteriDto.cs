using AtlasWeb.Models;

namespace AtlasWeb.DTOs
{
    public class MusteriDto
    {
        public string MusteriKodu { get; set; } = string.Empty;
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
    }
}
