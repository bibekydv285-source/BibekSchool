using System.ComponentModel.DataAnnotations;

namespace BibekSchool.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Username or email is required")]
        [Display(Name = "Username or Email")]
        public string UsernameOrEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember Me")]
        public bool RememberMe { get; set; }

        public string? ReturnUrl { get; set; }
    }
}