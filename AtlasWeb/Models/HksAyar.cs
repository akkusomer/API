namespace AtlasWeb.Models;

public sealed class HksAyar : BaseEntity
{
    public string KullaniciAdi { get; set; } = string.Empty;

    public string PasswordCipherText { get; set; } = string.Empty;

    public string ServicePasswordCipherText { get; set; } = string.Empty;
}
