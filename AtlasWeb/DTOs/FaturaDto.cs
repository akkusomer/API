namespace AtlasWeb.DTOs
{
    public class FaturaDetayDto
    {
        public Guid StokId { get; set; }
        public string? AlisKunye { get; set; }
        public string? SatisKunye { get; set; }
        public decimal Miktar { get; set; }
        public decimal BirimFiyat { get; set; }
    }

    public class FaturaDto
    {
        public Guid CariKartId { get; set; }
        public DateTime FaturaTarihi { get; set; } = DateTime.UtcNow;
        public string? AlisKunye { get; set; }
        public string? Aciklama { get; set; }
        public decimal TahsilEdilenTutar { get; set; }
        public List<FaturaDetayDto> Kalemler { get; set; } = new();
    }

    public class FaturaSatisKunyeTalepDto
    {
        public int BildirimciSifatId { get; set; }

        public int BildirimTuruId { get; set; }

        public int BelgeTipiId { get; set; }

        public string? BelgeNo { get; set; }
    }

    public class FaturaSatisKunyeKalemSonucDto
    {
        public Guid DetayId { get; set; }

        public Guid StokId { get; set; }

        public string? StokAdi { get; set; }

        public string? AlisKunye { get; set; }

        public string? SatisKunye { get; set; }
    }

    public class FaturaSatisKunyeSonucDto
    {
        public Guid FaturaId { get; set; }

        public string? FaturaNo { get; set; }

        public int IslenenKalemSayisi { get; set; }

        public string Mesaj { get; set; } = string.Empty;

        public List<FaturaSatisKunyeKalemSonucDto> Kalemler { get; set; } = new();
    }
}
