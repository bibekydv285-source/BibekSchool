using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BibekSchool.Models
{
    public class Student
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = null!;

        [Required]
        [StringLength(20)]
        [Display(Name = "Admission Number")]
        public string AdmissionNumber { get; set; } = string.Empty;

        [Display(Name = "Admission Date")]
        [DataType(DataType.Date)]
        public DateTime AdmissionDate { get; set; } = DateTime.UtcNow;

        [StringLength(50)]
        [Display(Name = "Roll Number")]
        public string? RollNumber { get; set; }

        public int? ClassId { get; set; }

        [ForeignKey("ClassId")]
        public virtual SchoolClass? Class { get; set; }

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

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Updated At")]
        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<Mark> Marks { get; set; } = new List<Mark>();
        public virtual ICollection<Result> Results { get; set; } = new List<Result>();
    }
}