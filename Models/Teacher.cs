using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BibekSchool.Models
{
    public class Teacher : ITrackableTimestamps
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = null!;

        [Required]
        [StringLength(20)]
        [Display(Name = "Employee ID")]
        public string EmployeeId { get; set; } = string.Empty;

        [Display(Name = "Joining Date")]
        [DataType(DataType.Date)]
        public DateTime JoiningDate { get; set; } = DateTime.UtcNow;

        [StringLength(100)]
        [Display(Name = "Qualification")]
        public string? Qualification { get; set; }

        [StringLength(100)]
        [Display(Name = "Specialization")]
        public string? Specialization { get; set; }

        [Display(Name = "Experience (Years)")]
        public int ExperienceYears { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Salary")]
        public decimal Salary { get; set; }

        [StringLength(50)]
        [Display(Name = "Designation")]
        public string? Designation { get; set; }

        [StringLength(20)]
        [Display(Name = "Emergency Contact")]
        public string? EmergencyContact { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Updated At")]
        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<TeacherAssignment> Assignments { get; set; } = new List<TeacherAssignment>();
        public virtual ICollection<Mark> Marks { get; set; } = new List<Mark>();
    }
}