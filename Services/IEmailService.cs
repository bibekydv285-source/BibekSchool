using System.Threading.Tasks;

namespace BibekSchool.Services
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string toEmail, string toName, string subject, string htmlBody, string? textBody = null);
        Task<bool> SendOtpEmailAsync(string toEmail, string toName, string otpCode, int expiryMinutes);
    }
}