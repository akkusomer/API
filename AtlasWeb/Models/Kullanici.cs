namespace AtlasWeb.Models
{
    public class Kullanici : BaseEntity
    {
        public string Ad { get; set; } = null!;
        public string Soyad { get; set; } = null!;
        public string EPosta { get; set; } = null!;
        public string? Telefon { get; set; }
        public string SifreHash { get; set; } = null!;
        public string Rol { get; set; } = KullaniciRol.User;
        public int FailedLoginCount { get; set; }
        public DateTime? LockoutEnd { get; set; }
        public ICollection<KullaniciToken> Tokens { get; set; } = [];
        public ICollection<KullaniciSifreSifirlamaToken> SifreSifirlamaTokenleri { get; set; } = [];
    }

    public static class KullaniciRol
    {
        public const string Admin = "Admin";
        public const string User = "User";
    }
}
