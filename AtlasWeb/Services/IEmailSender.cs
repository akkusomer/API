namespace AtlasWeb.Services
{
    public interface IEmailSender
    {
        bool IsConfigured { get; }
        Task SendPasswordResetAsync(string recipientEmail, string recipientName, string resetUrl, DateTime expiresAtUtc);
    }
}
