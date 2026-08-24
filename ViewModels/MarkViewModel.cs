using System.ComponentModel.DataAnnotations;
using BibekSchool.Models;

namespace BibekSchool.ViewModels
{
    public class MarkViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Student is required")]
        [Display(Name = "Student")]
        public int StudentId { get; set; }

        [Required(ErrorMessage = "Subject is required")]
        [Display(Name = "Subject")]
        public int SubjectId { get; set; }

        [Required(ErrorMessage = "Teacher is required")]
        [Display(Name = "Teacher")]
        public int TeacherId { get; set; }

        [Required(ErrorMessage = "Exam name is required")]
        [StringLength(100)]
        [Display(Name = "Exam Name")]
        public string ExamName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Academic year is required")]
        [StringLength(20)]
        [Display(Name = "Academic Year")]
        public string AcademicYear { get; set; } = string.Empty;

        [Display(Name = "Full Marks")]
        [Range(1, 1000)]
        public int FullMarks { get; set; } = 100;

        [Display(Name = "Pass Marks")]
        [Range(1, 1000)]
        public int PassMarks { get; set; } = 40;

        [Required(ErrorMessage = "Obtained marks is required")]
        [Display(Name = "Obtained Marks")]
        [Range(0, 1000)]
        public decimal ObtainedMarks { get; set; }

        [StringLength(5)]
        [Display(Name = "Grade")]
        public string? Grade { get; set; }

        [StringLength(500)]
        [Display(Name = "Remarks")]
        public string? Remarks { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Exam Date")]
        public DateTime ExamDate { get; set; } = DateTime.UtcNow;

        public string? StudentName { get; set; }
        public string? SubjectName { get; set; }
        public string? TeacherName { get; set; }
        public bool IsPassed => ObtainedMarks >= PassMarks;
        public decimal Percentage => FullMarks > 0 ? (ObtainedMarks / FullMarks) * 100 : 0;

        public int? ClassId { get; set; }
        public Student? Student { get; set; }
        public List<SchoolClass> Classes { get; set; } = new();
        public List<Student> Students { get; set; } = new();
        public List<Subject> Subjects { get; set; } = new();
        public List<Teacher> Teachers { get; set; } = new();
    }
}