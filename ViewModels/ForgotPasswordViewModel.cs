using System.ComponentModel.DataAnnotations;

namespace BibekSchool.ViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        public string EmailTrimmed => Email?.Trim() ?? string.Empty;
    }

    public class VerifyOtpViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = string.Empty;

        public string EmailTrimmed => Email?.Trim() ?? string.Empty;

        [Required(ErrorMessage = "Verification code is required")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Code must be 6 digits")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Code must be 6 digits")]
        [Display(Name = "Verification Code")]
        public string OtpCode { get; set; } = string.Empty;
    }

    public class ResetPasswordWithOtpViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = string.Empty;

        public string EmailTrimmed => Email?.Trim() ?? string.Empty;

        [Required(ErrorMessage = "Verification code is required")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Code must be 6 digits")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Code must be 6 digits")]
        [Display(Name = "Verification Code")]
        public string OtpCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your new password")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class ForgotPasswordResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Email { get; set; }
        public int? ResendCooldown { get; set; }
        public object? Errors { get; set; }
    }

    public class ResendOtpResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? CooldownSeconds { get; set; }
    }
}