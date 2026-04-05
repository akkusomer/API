namespace AtlasWeb.Services
{
    public static class IdentityNormalizer
    {
        public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
    }
}
