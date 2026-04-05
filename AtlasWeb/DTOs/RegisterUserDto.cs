namespace AtlasWeb.DTOs
{
    public class RegisterUserDto
    {
        public Guid MusteriId { get; set; } // Şirket ID'si
        public string Ad { get; set; } = string.Empty;
        public string Soyad { get; set; } = string.Empty;
        public string EPosta { get; set; } = string.Empty;
        public string? Telefon { get; set; }
        public string Sifre { get; set; } = string.Empty;
    }
}
