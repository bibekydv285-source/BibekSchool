using System.ComponentModel.DataAnnotations;

namespace BibekSchool.ViewModels
{
    public class RegisterStudentViewModel
    {
        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number")]
        [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = string.Empty;

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

        [Required(ErrorMessage = "Date of birth is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime DateOfBirth { get; set; }

        [Required(ErrorMessage = "Gender is required")]
        [StringLength(10)]
        [Display(Name = "Gender")]
        public string Gender { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
        [Display(Name = "Address")]
        public string? Address { get; set; }

        [Required(ErrorMessage = "Father's name is required")]
        [StringLength(100, ErrorMessage = "Father's name cannot exceed 100 characters")]
        [Display(Name = "Father's Name")]
        public string FatherName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mother's name is required")]
        [StringLength(100, ErrorMessage = "Mother's name cannot exceed 100 characters")]
        [Display(Name = "Mother's Name")]
        public string MotherName { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Invalid phone number")]
        [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
        [Display(Name = "Father's Phone")]
        public string? FatherPhone { get; set; }

        [Phone(ErrorMessage = "Invalid phone number")]
        [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
        [Display(Name = "Mother's Phone")]
        public string? MotherPhone { get; set; }

        [StringLength(500, ErrorMessage = "Guardian address cannot exceed 500 characters")]
        [Display(Name = "Guardian Address")]
        public string? GuardianAddress { get; set; }

        [StringLength(50, ErrorMessage = "Blood group cannot exceed 50 characters")]
        [Display(Name = "Blood Group")]
        public string? BloodGroup { get; set; }

        [StringLength(500, ErrorMessage = "Medical conditions cannot exceed 500 characters")]
        [Display(Name = "Medical Conditions")]
        public string? MedicalConditions { get; set; }
    }
}