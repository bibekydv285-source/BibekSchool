using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BibekSchool.Models
{
    public class PasswordResetToken : ITrackableTimestamps
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = null!;

        // FIX: was [StringLength(100)] — too short. ASP.NET Core Identity's
        // default DataProtectorTokenProvider generates a Base64-encoded,
        // encrypted token that is typically 150-250+ characters long
        // (it's not a short code — it's an encrypted payload). Saving it
        // into a column capped at 100 characters caused SQL Server error
        // 2628 ("String or binary data would be truncated"), which is what
        // crashed the ForgotPassword request. nvarchar(max) is used here to
        // avoid guessing at a "safe" fixed length, since the token length
        // isn't officially documented/guaranteed by Identity.
        [Required]
        public string Token { get; set; } = string.Empty;

        [StringLength(6)]
        public string? OtpCode { get; set; }

        [Display(Name = "Expiry Date")]
        public DateTime ExpiryDate { get; set; }

        [Display(Name = "Is Used")]
        public bool IsUsed { get; set; } = false;

        [Display(Name = "Otp Attempts")]
        public int OtpAttempts { get; set; } = 0;

        [Display(Name = "Last Otp Sent At")]
        public DateTime? LastOtpSentAt { get; set; }

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Updated At")]
        public DateTime? UpdatedAt { get; set; }

        [Display(Name = "Used At")]
        public DateTime? UsedAt { get; set; }
    }
}