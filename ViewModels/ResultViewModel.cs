using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BibekSchool.ViewModels
{
    public class ResultViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Student is required")]
        [Display(Name = "Student")]
        public int StudentId { get; set; }

        [Required(ErrorMessage = "Academic year is required")]
        [StringLength(20)]
        [Display(Name = "Academic Year")]
        public string AcademicYear { get; set; } = string.Empty;

        [Required(ErrorMessage = "Term is required")]
        [StringLength(50)]
        [Display(Name = "Term/Exam")]
        public string Term { get; set; } = string.Empty;

        [Display(Name = "Total Marks Obtained")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalObtainedMarks { get; set; }

        [Display(Name = "Total Full Marks")]
        public int TotalFullMarks { get; set; }

        [Display(Name = "Percentage")]
        [Column(TypeName = "decimal(5,2)")]
        public decimal Percentage { get; set; }

        [StringLength(5)]
        [Display(Name = "Overall Grade")]
        public string? OverallGrade { get; set; }

        [Display(Name = "Rank in Class")]
        public int? RankInClass { get; set; }

        [Display(Name = "Total Students in Class")]
        public int? TotalStudentsInClass { get; set; }

        [Display(Name = "Is Passed")]
        public bool IsPassed { get; set; }

        [StringLength(1000)]
        [Display(Name = "Remarks")]
        public string? Remarks { get; set; }

        [Display(Name = "Published Date")]
        public DateTime? PublishedDate { get; set; }

        [Display(Name = "Published By")]
        public string? PublishedBy { get; set; }

        public string? StudentName { get; set; }
        public string? ClassName { get; set; }
        public List<MarkViewModel> SubjectMarks { get; set; } = new();
    }
}