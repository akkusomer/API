namespace AtlasWeb.Models
{
    public class Kullanici : BaseEntity
    {
        // Id buraya Base'den geliyor, tekrar yazmiyoruz!
        public string Ad { get; set; } = null!;
        public string Soyad { get; set; } = null!;
        public string EPosta { get; set; } = null!;
        public string SifreHash { get; set; } = null!;
        public string Rol { get; set; } = KullaniciRol.User; // Varsayılan rol: User
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }
    }

    /// <summary>
    /// Kullanıcı rolleri — Magic string yerine sabit değerler (JWT ve EF uyumluluğu için string sabit)
    /// </summary>
    public static class KullaniciRol
    {
        public const string Admin = "Admin";
        public const string User = "User";
    }
}