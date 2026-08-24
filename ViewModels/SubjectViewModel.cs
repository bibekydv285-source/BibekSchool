using System.ComponentModel.DataAnnotations;
using BibekSchool.Models;

namespace BibekSchool.ViewModels
{
    public class SubjectViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Subject name is required")]
        [StringLength(100)]
        [Display(Name = "Subject Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Subject code is required")]
        [StringLength(20)]
        [Display(Name = "Subject Code")]
        public string Code { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Is Core Subject")]
        public bool IsCoreSubject { get; set; } = true;

        [Display(Name = "Full Marks")]
        [Range(1, 1000, ErrorMessage = "Full marks must be between 1 and 1000")]
        public int FullMarks { get; set; } = 100;

        [Display(Name = "Pass Marks")]
        [Range(1, 1000, ErrorMessage = "Pass marks must be between 1 and 1000")]
        public int PassMarks { get; set; } = 40;

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        public List<int> AssignedClassIds { get; set; } = new();
        public List<SchoolClass> Classes { get; set; } = new();
    }
}