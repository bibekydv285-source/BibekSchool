using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using BibekSchool.Services;

namespace BibekSchool.Services
{
    public class EmailSettings
    {
        public string SmtpServer { get; set; } = string.Empty;
        public int SmtpPort { get; set; } = 587;
        public string SmtpUsername { get; set; } = string.Empty;
        public string SmtpPassword { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = string.Empty;
        public bool EnableSsl { get; set; } = true;
        public bool UseMock { get; set; } = true;
    }

    public class PasswordResetSettings
    {
        public int OtpExpiryMinutes { get; set; } = 10;
        public int ResendCooldownSeconds { get; set; } = 60;
        public int MaxAttemptsPerHour { get; set; } = 3;
        public int MaxOtpAttempts { get; set; } = 5;
    }

    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly PasswordResetSettings _resetSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(
            IOptions<EmailSettings> emailSettings,
            IOptions<PasswordResetSettings> resetSettings,
            ILogger<EmailService> logger)
        {
            _emailSettings = emailSettings.Value;
            _resetSettings = resetSettings.Value;
            _logger = logger;
        }

        public async Task<bool> SendEmailAsync(string toEmail, string toName, string subject, string htmlBody, string? textBody = null)
        {
            if (_emailSettings.UseMock)
            {
                _logger.LogInformation("MOCK EMAIL SENT - To: {ToEmail}, Subject: {Subject}, Body: {Body}", toEmail, subject, htmlBody);
                return true;
            }

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.FromEmail));
                message.To.Add(new MailboxAddress(toName, toEmail));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = htmlBody,
                    TextBody = textBody ?? StripHtml(htmlBody)
                };

                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.SmtpPort, _emailSettings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);
                await client.AuthenticateAsync(_emailSettings.SmtpUsername, _emailSettings.SmtpPassword);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Email sent successfully to {ToEmail}", toEmail);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {ToEmail}", toEmail);
                return false;
            }
        }

        public async Task<bool> SendOtpEmailAsync(string toEmail, string toName, string otpCode, int expiryMinutes)
        {
            var subject = "Your Password Reset Code - Bibek School";
            var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
</head>
<body style='font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;'>
    <div style='background: linear-gradient(135deg, #4f6ef7, #3d5ae0); padding: 30px; border-radius: 12px 12px 0 0; text-align: center;'>
        <h1 style='color: white; margin: 0; font-size: 24px;'>Bibek School</h1>
        <p style='color: rgba(255,255,255,0.9); margin: 10px 0 0;'>Password Reset Verification</p>
    </div>
    <div style='background: #f8fafc; padding: 30px; border-radius: 0 0 12px 12px; border: 1px solid #e2e8f0; border-top: none;'>
        <p style='font-size: 16px; margin-bottom: 20px;'>Hello <strong>{toName}</strong>,</p>
        <p style='font-size: 16px; margin-bottom: 20px;'>You requested to reset your password. Use the verification code below:</p>
        <div style='background: white; border: 2px solid #4f6ef7; border-radius: 8px; padding: 20px; text-align: center; margin: 20px 0;'>
            <span style='font-size: 32px; font-weight: 700; color: #4f6ef7; letter-spacing: 8px; font-family: monospace;'>{otpCode}</span>
        </div>
        <p style='font-size: 14px; color: #64748b; margin-bottom: 10px;'>This code will expire in <strong>{expiryMinutes} minutes</strong>.</p>
        <p style='font-size: 14px; color: #64748b;'>If you didn't request this, please ignore this email or contact support.</p>
        <hr style='border: none; border-top: 1px solid #e2e8f0; margin: 20px 0;'>
        <p style='font-size: 12px; color: #94a3b8; margin: 0;'>Bibek School - Secure Password Reset</p>
    </div>
</body>
</html>";

            var textBody = $@"
Bibek School - Password Reset Verification

Hello {toName},

You requested to reset your password. Use the verification code below:

{otpCode}

This code will expire in {expiryMinutes} minutes.

If you didn't request this, please ignore this email or contact support.

Bibek School
";

            return await SendEmailAsync(toEmail, toName, subject, htmlBody, textBody);
        }

        private static string StripHtml(string html)
        {
            return System.Text.RegularExpressions.Regex.Replace(html, "<[^>]*>", string.Empty);
        }
    }
}