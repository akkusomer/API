using AtlasWeb.Services;

namespace AtlasWeb.Tests.Support;

internal sealed class TestEmailSender : IEmailSender
{
    public bool IsConfigured { get; set; } = true;
    public List<(string RecipientEmail, string RecipientName, string ResetUrl, DateTime ExpiresAtUtc)> SentMessages { get; } = [];

    public Task SendPasswordResetAsync(string recipientEmail, string recipientName, string resetUrl, DateTime expiresAtUtc)
    {
        SentMessages.Add((recipientEmail, recipientName, resetUrl, expiresAtUtc));
        return Task.CompletedTask;
    }
}
