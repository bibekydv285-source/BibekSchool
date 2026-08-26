using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using BibekSchool.Models;

namespace BibekSchool.Services
{
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

            // Set default SMTP server if not configured
            if (string.IsNullOrWhiteSpace(_emailSettings.SmtpServer))
            {
                _emailSettings.SmtpServer = "smtp.gmail.com";
            }

            LogConfiguration();
        }

        private void LogConfiguration()
        {
            _logger.LogInformation("EmailService initialized - Server: {Server}:{Port}, Username: {Username}, FromEmail: {FromEmail}, UseMock: {UseMock}, EnableSsl: {EnableSsl}",
                _emailSettings.SmtpServer, _emailSettings.SmtpPort, _emailSettings.SmtpUsername, _emailSettings.FromEmail, _emailSettings.UseMock, _emailSettings.EnableSsl);

            if (!_emailSettings.IsValid())
            {
                _logger.LogWarning("Email configuration incomplete - some required settings are missing. Emails will not be sent unless UseMock is enabled.");
            }
        }

        // CHANGED: return type is now a tuple (Success, ErrorMessage) instead of just bool.
        // This lets the controller show the REAL error on screen for debugging,
        // instead of only a generic "failed" message.
        public async Task<(bool Success, string? ErrorMessage)> SendEmailAsync(string toEmail, string toName, string subject, string htmlBody, string? textBody = null)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                _logger.LogError("Cannot send email: recipient email is empty");
                return (false, "Recipient email is empty");
            }

            if (!IsValidEmail(toEmail))
            {
                _logger.LogError("Cannot send email: invalid recipient email format: {ToEmail}", toEmail);
                return (false, "Invalid recipient email format");
            }

            if (_emailSettings.UseMock)
            {
                _logger.LogInformation("MOCK EMAIL SENT - To: {ToEmail}, Subject: {Subject}", toEmail, subject);
                _logger.LogDebug("Mock email body: {Body}", htmlBody);
                return (true, null);
            }

            if (!_emailSettings.IsValid())
            {
                var missing = $"Server='{_emailSettings.SmtpServer}', User='{_emailSettings.SmtpUsername}', PasswordSet={!string.IsNullOrWhiteSpace(_emailSettings.SmtpPassword)}, From='{_emailSettings.FromEmail}'";
                _logger.LogError("Email configuration incomplete - {Missing}", missing);
                return (false, $"CONFIG_MISSING: {missing}");
            }

            try
            {
                _logger.LogDebug("Attempting to send email to {ToEmail} via {Server}:{Port}", toEmail, _emailSettings.SmtpServer, _emailSettings.SmtpPort);

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

                // Set timeout for connection and send operations
                client.Timeout = 30000;

                // Connect with proper SSL/TLS
                var secureSocketOptions = _emailSettings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
                _logger.LogDebug("Connecting to SMTP server with {SecureSocketOptions}", secureSocketOptions);
                await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.SmtpPort, secureSocketOptions);

                _logger.LogDebug("Authenticating as {Username}", _emailSettings.SmtpUsername);
                await client.AuthenticateAsync(_emailSettings.SmtpUsername, _emailSettings.SmtpPassword);

                _logger.LogDebug("Sending email to {ToEmail}", toEmail);
                await client.SendAsync(message);

                _logger.LogDebug("Disconnecting from SMTP server");
                await client.DisconnectAsync(true);

                _logger.LogInformation("Email sent successfully to {ToEmail} (Subject: {Subject})", toEmail, subject);
                return (true, null);
            }
            catch (MailKit.Net.Smtp.SmtpCommandException ex)
            {
                _logger.LogError(ex, "SMTP command failed sending to {ToEmail}: {StatusCode} - {Message}", toEmail, ex.StatusCode, ex.Message);
                return (false, $"SMTP_COMMAND: {ex.StatusCode} - {ex.Message}");
            }
            catch (MailKit.Net.Smtp.SmtpProtocolException ex)
            {
                _logger.LogError(ex, "SMTP protocol error sending to {ToEmail}: {Message}", toEmail, ex.Message);
                return (false, $"SMTP_PROTOCOL: {ex.Message}");
            }
            catch (System.Net.Sockets.SocketException ex)
            {
                _logger.LogError(ex, "Network error connecting to SMTP server {Server}:{Port}: {Message}", _emailSettings.SmtpServer, _emailSettings.SmtpPort, ex.Message);
                return (false, $"NETWORK: {ex.Message}");
            }
            catch (System.Security.Authentication.AuthenticationException ex)
            {
                _logger.LogError(ex, "Authentication failed for {Username} on {Server}:{Port}: {Message}", _emailSettings.SmtpUsername, _emailSettings.SmtpServer, _emailSettings.SmtpPort, ex.Message);
                return (false, $"AUTH: {ex.Message}");
            }
            catch (System.OperationCanceledException ex)
            {
                _logger.LogError(ex, "Email send operation timed out for {ToEmail}", toEmail);
                return (false, $"TIMEOUT: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error sending email to {ToEmail}: {Message}", toEmail, ex.Message);
                return (false, $"UNEXPECTED: {ex.GetType().Name} - {ex.Message}");
            }
        }

        public async Task<(bool Success, string? ErrorMessage)> SendOtpEmailAsync(string toEmail, string toName, string otpCode, int expiryMinutes)
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

            _logger.LogInformation("Sending OTP email to {ToEmail} with code {OtpCode} (expiry: {ExpiryMinutes} min)", toEmail, otpCode, expiryMinutes);
            return await SendEmailAsync(toEmail, toName, subject, htmlBody, textBody);
        }

        public async Task<(bool Success, string? ErrorMessage)> SendRegistrationConfirmationAsync(string toEmail, string toName, string role, string loginUrl, string? password = null)
        {
            var subject = $"Welcome to Bibek School - Your {role} Account Details";
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
        <p style='color: rgba(255,255,255,0.9); margin: 10px 0 0;'>Welcome to Our Community!</p>
    </div>
    <div style='background: #f8fafc; padding: 30px; border-radius: 0 0 12px 12px; border: 1px solid #e2e8f0; border-top: none;'>
        <p style='font-size: 16px; margin-bottom: 20px;'>Hello <strong>{toName}</strong>,</p>
        <p style='font-size: 16px; margin-bottom: 20px;'>Your {role.ToLower()} account has been created successfully. Here are your account details:</p>
        <div style='background: white; border: 2px solid #4f6ef7; border-radius: 8px; padding: 20px; margin: 20px 0;'>
            <p style='font-size: 14px; color: #64748b; margin: 5px 0;'><strong>Email:</strong> {toEmail}</p>
            <p style='font-size: 14px; color: #64748b; margin: 5px 0;'><strong>Role:</strong> {role}</p>
            {(password != null ? $"<p style='font-size: 14px; color: #64748b; margin: 5px 0;'><strong>Password:</strong> {password}</p>" : "")}
        </div>
        <p style='font-size: 14px; color: #64748b; margin-bottom: 10px;'>{(password != null ? "Please log in and change your password immediately after first login." : "You can now log in to your account using your email and password.")}</p>
        <p style='font-size: 14px; color: #64748b;'>You can access your account at: <a href='{loginUrl}' style='color: #4f6ef7;'>Bibek School Login</a></p>
        <hr style='border: none; border-top: 1px solid #e2e8f0; margin: 20px 0;'>
        <p style='font-size: 12px; color: #94a3b8; margin: 0;'>Bibek School Administration</p>
    </div>
</body>
</html>";

            var textBody = $@"
Bibek School - Welcome!

Hello {toName},

Your {role.ToLower()} account has been created successfully. Here are your account details:

Email: {toEmail}
Role: {role}
{(password != null ? $"Password: {password}" : "")}

{(password != null ? "Please log in and change your password immediately after first login." : "You can now log in to your account using your email and password.")}

You can access your account at: {loginUrl}

Bibek School Administration
";

            _logger.LogInformation("Sending registration confirmation email to {ToEmail} for role {Role}", toEmail, role);
            return await SendEmailAsync(toEmail, toName, subject, htmlBody, textBody);
        }

        private static string StripHtml(string html)
        {
            return System.Text.RegularExpressions.Regex.Replace(html, "<[^>]*>", string.Empty);
        }

        private static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}