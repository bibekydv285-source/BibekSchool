using System.ComponentModel.DataAnnotations;

namespace BibekSchool.Models
{
    public class EmailSettings
    {
        [Required]
        public string SmtpServer { get; set; } = "smtp.gmail.com";

        [Range(1, 65535)]
        public int SmtpPort { get; set; } = 587;

        [Required]
        public string SmtpUsername { get; set; } = string.Empty;

        [Required]
        public string SmtpPassword { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string FromEmail { get; set; } = string.Empty;

        [Required]
        public string FromName { get; set; } = "Bibek School";

        public bool EnableSsl { get; set; } = true;

        public bool UseMock { get; set; } = false;

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(SmtpServer)
                && !string.IsNullOrWhiteSpace(SmtpUsername)
                && !string.IsNullOrWhiteSpace(SmtpPassword)
                && !string.IsNullOrWhiteSpace(FromEmail);
        }
    }

    public class PasswordResetSettings
    {
        public int OtpExpiryMinutes { get; set; } = 10;
        public int ResendCooldownSeconds { get; set; } = 60;
        public int MaxAttemptsPerHour { get; set; } = 3;
        public int MaxOtpAttempts { get; set; } = 5;
    }
}