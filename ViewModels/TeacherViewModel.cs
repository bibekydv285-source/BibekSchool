using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BibekSchool.Models;

namespace BibekSchool.ViewModels
{
    public class TeacherViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Phone]
        [StringLength(20)]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime? DateOfBirth { get; set; }

        [StringLength(10)]
        [Display(Name = "Gender")]
        public string Gender { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Address")]
        public string? Address { get; set; }

        [Required(ErrorMessage = "Employee ID is required")]
        [StringLength(20)]
        [Display(Name = "Employee ID")]
        public string EmployeeId { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        [Display(Name = "Joining Date")]
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

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm password is required")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;

        public List<int> AssignedClassIds { get; set; } = new();
        public List<int> AssignedSubjectIds { get; set; } = new();
        public List<SchoolClass> Classes { get; set; } = new();
        public List<Subject> Subjects { get; set; } = new();
    }
}