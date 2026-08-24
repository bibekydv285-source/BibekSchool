using System.ComponentModel.DataAnnotations;
using BibekSchool.Models;

namespace BibekSchool.ViewModels
{
    public class StudentViewModel
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

        [StringLength(20)]
        [Display(Name = "Admission Number")]
        public string AdmissionNumber { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        [Display(Name = "Admission Date")]
        public DateTime AdmissionDate { get; set; } = DateTime.UtcNow;

        [StringLength(50)]
        [Display(Name = "Roll Number")]
        public string? RollNumber { get; set; }

        [Display(Name = "Class")]
        public int? ClassId { get; set; }

        [StringLength(100)]
        [Display(Name = "Father's Name")]
        public string? FatherName { get; set; }

        [StringLength(100)]
        [Display(Name = "Mother's Name")]
        public string? MotherName { get; set; }

        [StringLength(20)]
        [Display(Name = "Father's Phone")]
        public string? FatherPhone { get; set; }

        [StringLength(20)]
        [Display(Name = "Mother's Phone")]
        public string? MotherPhone { get; set; }

        [StringLength(500)]
        [Display(Name = "Guardian Address")]
        public string? GuardianAddress { get; set; }

        [StringLength(50)]
        [Display(Name = "Blood Group")]
        public string? BloodGroup { get; set; }

        [StringLength(500)]
        [Display(Name = "Medical Conditions")]
        public string? MedicalConditions { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        public string? ClassName { get; set; }
        public string? ProfileImage { get; set; }
        public List<SchoolClass> Classes { get; set; } = new();
    }
}