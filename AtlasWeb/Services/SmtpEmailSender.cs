using System.Net;
using System.Net.Mail;
using System.Text;

namespace AtlasWeb.Services
{
    public class SmtpEmailSender : IEmailSender
    {
        private static readonly TimeZoneInfo IstanbulTimeZone = ResolveIstanbulTimeZone();

        private readonly IConfiguration _configuration;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(_configuration["Email:SmtpHost"]) &&
            !string.IsNullOrWhiteSpace(_configuration["Email:FromAddress"]);

        public async Task SendPasswordResetAsync(
            string recipientEmail,
            string recipientName,
            string resetUrl,
            DateTime expiresAtUtc)
        {
            if (!IsConfigured)
            {
                throw new InvalidOperationException("E-posta servisi yapilandirilmamis.");
            }

            var smtpHost = _configuration["Email:SmtpHost"]!;
            var smtpPort = _configuration.GetValue("Email:Port", 587);
            var smtpUser = _configuration["Email:Username"];
            var smtpPassword = _configuration["Email:Password"];
            var useSsl = _configuration.GetValue("Email:UseSsl", true);
            var fromAddress = _configuration["Email:FromAddress"]!;
            var fromName = _configuration["Email:FromName"] ?? "AtlasWeb";
            var expiresText = TimeZoneInfo
                .ConvertTimeFromUtc(DateTime.SpecifyKind(expiresAtUtc, DateTimeKind.Utc), IstanbulTimeZone)
                .ToString("dd.MM.yyyy HH:mm");

            using var message = new MailMessage
            {
                From = new MailAddress(fromAddress, fromName, Encoding.UTF8),
                Subject = "AtlasWeb sifre sifirlama baglantisi",
                SubjectEncoding = Encoding.UTF8,
                BodyEncoding = Encoding.UTF8,
                IsBodyHtml = true,
                Body = $"""
                    <div style="font-family: Arial, sans-serif; line-height: 1.6; color: #111827;">
                        <h2 style="margin-bottom: 16px;">Sifre sifirlama talebi</h2>
                        <p>Merhaba {WebUtility.HtmlEncode(recipientName)},</p>
                        <p>AtlasWeb hesabiniz icin sifre sifirlama talebi aldik.</p>
                        <p style="margin: 24px 0;">
                            <a href="{WebUtility.HtmlEncode(resetUrl)}"
                               style="background: #2563eb; color: white; text-decoration: none; padding: 12px 20px; border-radius: 8px; display: inline-block;">
                                Sifremi Yenile
                            </a>
                        </p>
                        <p>Bu baglanti <strong>{WebUtility.HtmlEncode(expiresText)}</strong> tarihine kadar gecerlidir.</p>
                        <p>Eger bu istegi siz yapmadiysaniz bu e-postayi dikkate almayabilirsiniz.</p>
                    </div>
                    """
            };

            message.To.Add(new MailAddress(recipientEmail, recipientName, Encoding.UTF8));

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                EnableSsl = useSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false
            };

            if (!string.IsNullOrWhiteSpace(smtpUser))
            {
                client.Credentials = new NetworkCredential(smtpUser, smtpPassword);
            }
            else
            {
                client.UseDefaultCredentials = true;
            }

            await client.SendMailAsync(message);

            _logger.LogInformation(
                "Password reset email sent. Recipient: {Recipient} | ExpiresAtUtc: {ExpiresAtUtc}",
                recipientEmail,
                expiresAtUtc);
        }

        private static TimeZoneInfo ResolveIstanbulTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
            }
        }
    }
}
