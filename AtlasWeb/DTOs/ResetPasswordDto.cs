namespace AtlasWeb.DTOs
{
    public class ResetPasswordDto
    {
        public string Token { get; set; } = string.Empty;
        public string YeniSifre { get; set; } = string.Empty;
        public string YeniSifreTekrar { get; set; } = string.Empty;
    }
}
