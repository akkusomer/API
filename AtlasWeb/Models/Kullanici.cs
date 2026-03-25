namespace AtlasWeb.Models
{
    public class Kullanici : BaseEntity
    {
        // Id BaseEntity'den geliyor
        public string Ad { get; set; } = null!;
        public string Soyad { get; set; } = null!;
        public string EPosta { get; set; } = null!;
        public string SifreHash { get; set; } = null!;
        public string Rol { get; set; } = KullaniciRol.User;

        // 🛡️ Brute-Force Koruması
        public int FailedLoginCount { get; set; } = 0;
        public DateTime? LockoutEnd { get; set; }

        // Navigation: Bu kullanıcıya ait tüm refresh token kayıtları
        public ICollection<KullaniciToken> Tokens { get; set; } = [];
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