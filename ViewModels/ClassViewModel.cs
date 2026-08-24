using System.ComponentModel.DataAnnotations;
using BibekSchool.Models;

namespace BibekSchool.ViewModels
{
    public class ClassViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Class name is required")]
        [StringLength(50)]
        [Display(Name = "Class Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(10)]
        [Display(Name = "Section")]
        public string? Section { get; set; }

        [StringLength(200)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Class Teacher")]
        public int? ClassTeacherId { get; set; }

        [Display(Name = "Capacity")]
        [Range(1, 100, ErrorMessage = "Capacity must be between 1 and 100")]
        public int Capacity { get; set; } = 40;

        [Required(ErrorMessage = "Academic year is required")]
        [StringLength(20)]
        [Display(Name = "Academic Year")]
        public string AcademicYear { get; set; } = string.Empty;

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        public string? ClassTeacherName { get; set; }
        public int StudentCount { get; set; }
        public List<Teacher> Teachers { get; set; } = new();
        public List<Subject> Subjects { get; set; } = new();
        public List<int> SelectedSubjectIds { get; set; } = new();
    }
}