using AtlasWeb.DTOs;

namespace AtlasWeb.Models;

public sealed class HksReferansKunyeKayit : BaseEntity
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

    public string ReferansKunyelerJson { get; set; } = "[]";
}
