using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BibekSchool.Models
{
    public class AuditLog : ITrackableTimestamps
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = null!;

        [Required]
        [StringLength(100)]
        [Display(Name = "Action")]
        public string Action { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "Entity Type")]
        public string EntityType { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Entity Id")]
        public string? EntityId { get; set; }

        [Display(Name = "Old Values")]
        public string? OldValues { get; set; }

        [Display(Name = "New Values")]
        public string? NewValues { get; set; }

        [StringLength(450)]
        [Display(Name = "IP Address")]
        public string? IpAddress { get; set; }

        [StringLength(500)]
        [Display(Name = "User Agent")]
        public string? UserAgent { get; set; }

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Updated At")]
        public DateTime? UpdatedAt { get; set; }
    }
}