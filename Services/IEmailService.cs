using System.Threading.Tasks;

namespace BibekSchool.Services
{
    public interface IEmailService
    {
        Task<(bool Success, string? ErrorMessage)> SendEmailAsync(string toEmail, string toName, string subject, string htmlBody, string? textBody = null);
        Task<(bool Success, string? ErrorMessage)> SendOtpEmailAsync(string toEmail, string toName, string otpCode, int expiryMinutes);
        Task<(bool Success, string? ErrorMessage)> SendRegistrationConfirmationAsync(string toEmail, string toName, string role, string loginUrl, string? password = null);
    }
}