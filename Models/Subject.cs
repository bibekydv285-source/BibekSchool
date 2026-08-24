using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BibekSchool.Models
{
    public class Subject
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Subject Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        [Display(Name = "Subject Code")]
        public string Code { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Is Core Subject")]
        public bool IsCoreSubject { get; set; } = true;

        [Display(Name = "Full Marks")]
        public int FullMarks { get; set; } = 100;

        [Display(Name = "Pass Marks")]
        public int PassMarks { get; set; } = 40;

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Updated At")]
        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<ClassSubject> ClassSubjects { get; set; } = new List<ClassSubject>();
        public virtual ICollection<TeacherAssignment> TeacherAssignments { get; set; } = new List<TeacherAssignment>();
        public virtual ICollection<Mark> Marks { get; set; } = new List<Mark>();
    }
}