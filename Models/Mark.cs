using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BibekSchool.Models
{
    public class Mark
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int StudentId { get; set; }

        [ForeignKey("StudentId")]
        public virtual Student Student { get; set; } = null!;

        [Required]
        public int SubjectId { get; set; }

        [ForeignKey("SubjectId")]
        public virtual Subject Subject { get; set; } = null!;

        [Required]
        public int TeacherId { get; set; }

        [ForeignKey("TeacherId")]
        public virtual Teacher Teacher { get; set; } = null!;

        [Required]
        [StringLength(100)]
        [Display(Name = "Exam Name")]
        public string ExamName { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        [Display(Name = "Academic Year")]
        public string AcademicYear { get; set; } = string.Empty;

        [Display(Name = "Full Marks")]
        public int FullMarks { get; set; } = 100;

        [Display(Name = "Pass Marks")]
        public int PassMarks { get; set; } = 40;

        [Display(Name = "Obtained Marks")]
        [Range(0, 1000)]
        public decimal ObtainedMarks { get; set; }

        [StringLength(5)]
        [Display(Name = "Grade")]
        public string? Grade { get; set; }

        [StringLength(500)]
        [Display(Name = "Remarks")]
        public string? Remarks { get; set; }

        [Display(Name = "Exam Date")]
        [DataType(DataType.Date)]
        public DateTime ExamDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Updated At")]
        public DateTime? UpdatedAt { get; set; }

        [NotMapped]
        public bool IsPassed => ObtainedMarks >= PassMarks;

        [NotMapped]
        public decimal Percentage => FullMarks > 0 ? (ObtainedMarks / FullMarks) * 100 : 0;
    }
}