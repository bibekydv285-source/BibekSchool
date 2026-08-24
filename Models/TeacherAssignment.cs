using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BibekSchool.Models
{
    public class TeacherAssignment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TeacherId { get; set; }

        [ForeignKey("TeacherId")]
        public virtual Teacher Teacher { get; set; } = null!;

        [Required]
        public int ClassId { get; set; }

        [ForeignKey("ClassId")]
        public virtual SchoolClass Class { get; set; } = null!;

        [Required]
        public int SubjectId { get; set; }

        [ForeignKey("SubjectId")]
        public virtual Subject Subject { get; set; } = null!;

        [Display(Name = "Academic Year")]
        [StringLength(20)]
        public string AcademicYear { get; set; } = string.Empty;

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Assigned Date")]
        public DateTime AssignedDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Updated At")]
        public DateTime? UpdatedAt { get; set; }
    }
}