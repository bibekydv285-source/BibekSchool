using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BibekSchool.Models
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Title")]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Message")]
        public string Message { get; set; } = string.Empty;

        [StringLength(20)]
        [Display(Name = "Type")]
        public string Type { get; set; } = "Info";

        [StringLength(450)]
        [Display(Name = "Target User Id")]
        public string? TargetUserId { get; set; }

        [ForeignKey("TargetUserId")]
        public virtual ApplicationUser? TargetUser { get; set; }

        [StringLength(20)]
        [Display(Name = "Target Role")]
        public string? TargetRole { get; set; }

        [Display(Name = "Is Read")]
        public bool IsRead { get; set; } = false;

        [Display(Name = "Read At")]
        public DateTime? ReadAt { get; set; }

        [Display(Name = "Is Global")]
        public bool IsGlobal { get; set; } = false;

        [StringLength(500)]
        [Display(Name = "Reference Link")]
        public string? ReferenceLink { get; set; }

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [StringLength(450)]
        [Display(Name = "Created By")]
        public string? CreatedBy { get; set; }
    }
}