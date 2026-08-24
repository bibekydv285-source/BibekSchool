using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BibekSchool.Models
{
    public class SchoolClass
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Class Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(10)]
        [Display(Name = "Section")]
        public string? Section { get; set; }

        [StringLength(200)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        public int? ClassTeacherId { get; set; }

        [ForeignKey("ClassTeacherId")]
        public virtual Teacher? ClassTeacher { get; set; }

        [Display(Name = "Capacity")]
        public int Capacity { get; set; } = 40;

        [Display(Name = "Academic Year")]
        [StringLength(20)]
        public string AcademicYear { get; set; } = string.Empty;

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Updated At")]
        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<Student> Students { get; set; } = new List<Student>();
        public virtual ICollection<ClassSubject> ClassSubjects { get; set; } = new List<ClassSubject>();
        public virtual ICollection<TeacherAssignment> TeacherAssignments { get; set; } = new List<TeacherAssignment>();
    }
}