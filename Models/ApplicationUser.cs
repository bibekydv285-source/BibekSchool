using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace BibekSchool.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [StringLength(20)]
        [Display(Name = "Phone Number")]
        public override string? PhoneNumber { get; set; }

        [StringLength(500)]
        [Display(Name = "Address")]
        public string? Address { get; set; }

        [Display(Name = "Date of Birth")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [StringLength(10)]
        [Display(Name = "Gender")]
        public string? Gender { get; set; }

        [StringLength(500)]
        [Display(Name = "Profile Image")]
        public string? ProfileImage { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Updated At")]
        public DateTime? UpdatedAt { get; set; }

        [Display(Name = "Last Login")]
        public DateTime? LastLoginAt { get; set; }

        [NotMapped]
        public string Role { get; set; } = string.Empty;

        [JsonIgnore]
        public virtual Student? Student { get; set; }

        [JsonIgnore]
        public virtual Teacher? Teacher { get; set; }
    }
}