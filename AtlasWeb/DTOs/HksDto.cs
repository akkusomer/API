namespace AtlasWeb.DTOs;

public static class HksReferansKunyeDurum
{
    public const string Bos = "Bos";
    public const string Kuyrukta = "Kuyrukta";
    public const string Isleniyor = "Isleniyor";
    public const string Tamamlandi = "Tamamlandi";
    public const string Hatali = "Hatali";
}

public sealed class HksErrorDto
{
    public int HataKodu { get; set; }

    public string? Mesaj { get; set; }
}

public sealed class HksSelectOptionDto
{
    public int Id { get; set; }

    public string Ad { get; set; } = string.Empty;
}

public sealed class HksBildirimciBilgileriDto
{
    public int KisiSifat { get; set; }
}

public sealed class HksIkinciKisiBilgileriDto
{
    public int KisiSifat { get; set; }

    public string? TcKimlikVergiNo { get; set; }

    public string? DogumTarihi { get; set; }

    public string? AdSoyad { get; set; }

    public string? Eposta { get; set; }

    public string? CepTel { get; set; }

    public bool YurtDisiMi { get; set; }
}

public sealed class HksMalGidecekYerBilgileriDto
{
    public int GidecekYerIsletmeTuruId { get; set; }

    public int? GidecekIsyeriId { get; set; }

    public int? GidecekUlkeId { get; set; }

    public int GidecekYerIlId { get; set; }

    public int GidecekYerIlceId { get; set; }

    public int GidecekYerBeldeId { get; set; }

    public string? BelgeNo { get; set; }

    public int BelgeTipi { get; set; }

    public string? AracPlakaNo { get; set; }
}

public sealed class HksBildirimMalBilgileriDto
{
    public int UretimIlId { get; set; }

    public int UretimIlceId { get; set; }

    public int UretimBeldeId { get; set; }

    public int MalinNiteligi { get; set; }

    public int MalinKodNo { get; set; }

    public int UretimSekli { get; set; }

    public int MalinCinsiId { get; set; }

    public int MiktarBirimId { get; set; }

    public double MalinMiktari { get; set; }

    public double MalinSatisFiyat { get; set; }

    public int? GelenUlkeId { get; set; }

    public bool AnalizeGonderilecekMi { get; set; }
}

public sealed class HksBildirimKayitItemDto
{
    public string UniqueId { get; set; } = string.Empty;

    public Guid? FaturaDetayId { get; set; }

    public int BildirimTuru { get; set; }

    public long ReferansBildirimKunyeNo { get; set; }

    public HksBildirimciBilgileriDto BildirimciBilgileri { get; set; } = new();

    public HksIkinciKisiBilgileriDto IkinciKisiBilgileri { get; set; } = new();

    public HksMalGidecekYerBilgileriDto MalinGidecekYerBilgileri { get; set; } = new();

    public HksBildirimMalBilgileriDto BildirimMalBilgileri { get; set; } = new();
}

public sealed class HksBildirimKayitSonucDto
{
    public string? UniqueId { get; set; }

    public Guid? FaturaDetayId { get; set; }

    public long YeniKunyeNo { get; set; }

    public DateTime? KayitTarihi { get; set; }

    public int? MalinKodNo { get; set; }

    public int? MalinCinsiId { get; set; }

    public int? UretimSekli { get; set; }

    public int? UretimIlId { get; set; }

    public int? UretimIlceId { get; set; }

    public int? UretimBeldeId { get; set; }

    public string? UreticisininAdUnvani { get; set; }

    public string? MalinSahibAdi { get; set; }

    public double? MalinMiktari { get; set; }

    public int? MiktarBirimId { get; set; }

    public string? AracPlakaNo { get; set; }

    public double? RusumMiktari { get; set; }

    public string? BelgeNo { get; set; }

    public int? BelgeTipi { get; set; }
}

public sealed class HksBildirimKayitResponseDto
{
    public string? IslemKodu { get; set; }

    public int? HataKodu { get; set; }

    public string? Mesaj { get; set; }

    public List<HksErrorDto> HataKodlari { get; set; } = [];

    public List<HksBildirimKayitSonucDto> Sonuclar { get; set; } = [];
}

public sealed class HksSifatKayitDto
{
    public Guid Id { get; set; }

    public int HksSifatId { get; set; }

    public string Ad { get; set; } = string.Empty;

    public DateTime? GuncellemeTarihi { get; set; }
}

public sealed class HksKayitliKisiSorguDto
{
    public string TcKimlikVergiNo { get; set; } = string.Empty;

    public bool KayitliKisiMi { get; set; }

    public List<int> SifatIds { get; set; } = [];
}

public sealed class HksKayitliKisiSifatSonucDto
{
    public string TcKimlikVergiNo { get; set; } = string.Empty;

    public bool KayitliKisiMi { get; set; }

    public List<HksSelectOptionDto> Sifatlar { get; set; } = [];

    public List<HksHalIciIsyeriDto> HalIciIsyerleri { get; set; } = [];

    public List<int> BilinmeyenSifatIdler { get; set; } = [];
}

public sealed class HksHalIciIsyeriDto
{
    public int Id { get; set; }

    public int? HalId { get; set; }

    public string? HalAdi { get; set; }

    public string Ad { get; set; } = string.Empty;

    public string? TcKimlikVergiNo { get; set; }
}

public sealed class HksUrunKayitDto
{
    public Guid Id { get; set; }

    public int HksUrunId { get; set; }

    public string Ad { get; set; } = string.Empty;

    public DateTime? GuncellemeTarihi { get; set; }
}

public sealed class HksUrunBirimKayitDto
{
    public Guid Id { get; set; }

    public int HksUrunBirimId { get; set; }

    public string Ad { get; set; } = string.Empty;

    public DateTime? GuncellemeTarihi { get; set; }
}

public sealed class HksIsletmeTuruKayitDto
{
    public Guid Id { get; set; }

    public int HksIsletmeTuruId { get; set; }

    public string Ad { get; set; } = string.Empty;

    public DateTime? GuncellemeTarihi { get; set; }
}

public sealed class HksUretimSekliKayitDto
{
    public Guid Id { get; set; }

    public int HksUretimSekliId { get; set; }

    public string Ad { get; set; } = string.Empty;

    public DateTime? GuncellemeTarihi { get; set; }
}

public sealed class HksUrunCinsiDto
{
    public int HksUrunCinsiId { get; set; }

    public int HksUrunId { get; set; }

    public int? HksUretimSekliId { get; set; }

    public string Ad { get; set; } = string.Empty;

    public string? UrunKodu { get; set; }

    public bool? IthalMi { get; set; }
}

public sealed class HksUrunCinsiKayitDto
{
    public Guid Id { get; set; }

    public int HksUrunCinsiId { get; set; }

    public int HksUrunId { get; set; }

    public int? HksUretimSekliId { get; set; }

    public string Ad { get; set; } = string.Empty;

    public string? UrunKodu { get; set; }

    public bool? IthalMi { get; set; }

    public DateTime? GuncellemeTarihi { get; set; }
}

public sealed class HksIlKayitDto
{
    public Guid Id { get; set; }

    public int HksIlId { get; set; }

    public string Ad { get; set; } = string.Empty;

    public DateTime? GuncellemeTarihi { get; set; }
}

public sealed class HksIlceKayitDto
{
    public Guid Id { get; set; }

    public int HksIlceId { get; set; }

    public int HksIlId { get; set; }

    public string Ad { get; set; } = string.Empty;

    public DateTime? GuncellemeTarihi { get; set; }
}

public sealed class HksBeldeKayitDto
{
    public Guid Id { get; set; }

    public int HksBeldeId { get; set; }

    public int HksIlceId { get; set; }

    public string Ad { get; set; } = string.Empty;

    public DateTime? GuncellemeTarihi { get; set; }
}

public sealed class HksIlTopluSenkronSonucDto
{
    public Guid KaynakMusteriId { get; set; }

    public int SirketSayisi { get; set; }

    public int IlSayisi { get; set; }

    public DateTime GuncellemeTarihi { get; set; }
}

public sealed class HksIlceTopluSenkronSonucDto
{
    public Guid KaynakMusteriId { get; set; }

    public int SirketSayisi { get; set; }

    public int IlceSayisi { get; set; }

    public DateTime GuncellemeTarihi { get; set; }
}

public sealed class HksBeldeTopluSenkronSonucDto
{
    public Guid KaynakMusteriId { get; set; }

    public int SirketSayisi { get; set; }

    public int BeldeSayisi { get; set; }

    public DateTime GuncellemeTarihi { get; set; }
}

public sealed class HksReferansKunyeRequestDto
{
    public string? MalinSahibiTcKimlikVergiNo { get; set; }

    public long? KunyeNo { get; set; }

    public bool KalanMiktariSifirdanBuyukOlanlar { get; set; } = true;

    public DateTime? BaslangicTarihi { get; set; }

    public DateTime? BitisTarihi { get; set; }

    public int? KisiSifat { get; set; }

    public int? UrunId { get; set; }
}

public sealed class HksReferansKunyeDto
{
    public long KunyeNo { get; set; }

    public DateTime? BildirimTarihi { get; set; }

    public int? MalinKodNo { get; set; }

    public string? MalinAdi { get; set; }

    public int? MalinCinsKodNo { get; set; }

    public string? MalinCinsi { get; set; }

    public int? MalinTuruKodNo { get; set; }

    public string? MalinTuru { get; set; }

    public double? MalinMiktari { get; set; }

    public double? KalanMiktar { get; set; }

    public int? MiktarBirimId { get; set; }

    public string? MiktarBirimiAd { get; set; }

    public int? UretimIlId { get; set; }

    public int? UretimIlceId { get; set; }

    public int? UretimBeldeId { get; set; }

    public int? GidecekYerIsletmeTuruId { get; set; }

    public int? GidecekIsyeriId { get; set; }

    public int? GidecekYerIlId { get; set; }

    public int? GidecekYerIlceId { get; set; }

    public int? GidecekYerBeldeId { get; set; }

    public string? BelgeNo { get; set; }

    public int? BelgeTipi { get; set; }

    public string? AracPlakaNo { get; set; }

    public string? MalinSahibiTcKimlikVergiNo { get; set; }

    public string? UreticiTcKimlikVergiNo { get; set; }

    public string? BildirimciTcKimlikVergiNo { get; set; }

    public int? Sifat { get; set; }

    public string? UniqueId { get; set; }
}

public sealed class HksReferansKunyeResponseDto
{
    public string? IslemKodu { get; set; }

    public int? HataKodu { get; set; }

    public string? Mesaj { get; set; }

    public List<HksErrorDto> HataKodlari { get; set; } = [];

    public List<HksReferansKunyeDto> ReferansKunyeler { get; set; } = [];
}

public sealed class HksSearchProgressDto
{
    public int CompletedSteps { get; set; }

    public int TotalSteps { get; set; }

    public int ProgressPercent { get; set; }

    public string Label { get; set; } = string.Empty;
}

public sealed class HksReferansKunyeKaydetDto
{
    public DateTime? BaslangicTarihi { get; set; }

    public DateTime? BitisTarihi { get; set; }

    public string? IslemKodu { get; set; }

    public string? Mesaj { get; set; }

    public List<HksReferansKunyeDto>? ReferansKunyeler { get; set; }
}

public sealed class HksReferansKunyeKayitDto
{
    public string Durum { get; set; } = HksReferansKunyeDurum.Bos;

    public int ProgressPercent { get; set; }

    public string? ProgressLabel { get; set; }

    public string? Hata { get; set; }

    public DateTime? BaslangicTarihi { get; set; }

    public DateTime? BitisTarihi { get; set; }

    public string? IslemKodu { get; set; }

    public string? Mesaj { get; set; }

    public int KayitSayisi { get; set; }

    public DateTime? GuncellemeTarihi { get; set; }

    public List<HksReferansKunyeDto> ReferansKunyeler { get; set; } = [];
}

public sealed class HksAyarDto
{
    public string? KullaniciAdi { get; set; }

    public bool HasPassword { get; set; }

    public bool HasServicePassword { get; set; }

    public DateTime? GuncellemeTarihi { get; set; }
}

public sealed class HksAyarKaydetDto
{
    public string? KullaniciAdi { get; set; }

    public string? Password { get; set; }

    public string? ServicePassword { get; set; }
}
