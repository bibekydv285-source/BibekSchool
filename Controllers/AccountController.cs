using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using BibekSchool.Data;
using BibekSchool.Models;
using BibekSchool.ViewModels;
using BibekSchool.Services;

namespace BibekSchool.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IStudentService _studentService;
        private readonly ITeacherService _teacherService;
        private readonly INotificationService _notificationService;
        private readonly IEmailService _emailService;
        private readonly ApplicationDbContext _context;
        private readonly PasswordResetSettings _resetSettings;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IStudentService studentService,
            ITeacherService teacherService,
            INotificationService notificationService,
            IEmailService emailService,
            ApplicationDbContext context,
            IOptions<PasswordResetSettings> resetSettings,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _studentService = studentService;
            _teacherService = teacherService;
            _notificationService = notificationService;
            _emailService = emailService;
            _context = context;
            _resetSettings = resetSettings.Value;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.UsernameOrEmail);
                if (user == null)
                {
                    user = await _userManager.FindByNameAsync(model.UsernameOrEmail);
                }

                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "Invalid username/email or password.");
                    return View(model);
                }

                if (!user.IsActive)
                {
                    ModelState.AddModelError(string.Empty, "Your account has been deactivated. Please contact administrator.");
                    return View(model);
                }

                var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    user.LastLoginAt = DateTime.UtcNow;
                    await _userManager.UpdateAsync(user);

                    await _notificationService.CreateNotificationAsync(
                        "Welcome back!",
                        "You have successfully logged in.",
                        user.Id,
                        null,
                        false,
                        null,
                        "System");

                    var roles = await _userManager.GetRolesAsync(user);
                    var role = roles.FirstOrDefault() ?? "Student";

                    return role switch
                    {
                        "MainAdmin" => RedirectToAction("Dashboard", "Admin"),
                        "Admin" => RedirectToAction("Dashboard", "Admin"),
                        "Teacher" => RedirectToAction("Dashboard", "Teacher"),
                        "Student" => RedirectToAction("Dashboard", "Student"),
                        _ => RedirectToAction("Dashboard", "Student")
                    };
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid username/email or password.");
                }
            }

            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterStudentViewModel model)
        {
            if (ModelState.IsValid)
            {
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "Email already registered.");
                    return View(model);
                }

                existingUser = await _userManager.FindByNameAsync(model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "Username already taken.");
                    return View(model);
                }

                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName,
                    PhoneNumber = model.PhoneNumber,
                    DateOfBirth = model.DateOfBirth,
                    Gender = model.Gender,
                    Address = model.Address,
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Student");

                    var student = new Student
                    {
                        UserId = user.Id,
                        AdmissionNumber = $"ADM{DateTime.Now:yyyyMMdd}{new Random().Next(1000, 9999)}",
                        AdmissionDate = DateTime.UtcNow,
                        FatherName = model.FatherName,
                        MotherName = model.MotherName,
                        FatherPhone = model.FatherPhone,
                        MotherPhone = model.MotherPhone,
                        GuardianAddress = model.GuardianAddress,
                        BloodGroup = model.BloodGroup,
                        MedicalConditions = model.MedicalConditions,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Students.Add(student);
                    await _context.SaveChangesAsync();

                    await _notificationService.CreateNotificationAsync(
                        "Welcome to Bibek School!",
                        "Your account has been created successfully. Please log in to continue.",
                        user.Id,
                        "Student",
                        false,
                        "/Account/Login",
                        "System");

                    TempData["Success"] = "Registration Successful! Your student account has been created. Please log in to continue.";
                    return RedirectToAction("Login", "Account");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // FIX: Added [FromBody] — the client sends this request as JSON
        // (Content-Type: application/json, body: JSON.stringify(data)).
        // Without [FromBody], ASP.NET Core's default model binder tries to
        // read from form fields/query string instead, so model.Email was
        // always null even though the user typed a valid email. This was
        // the root cause of the "Email is required; Invalid email address"
        // error shown in the screenshot.
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordViewModel model)
        {
            // TEMP DIAGNOSTIC WRAPPER — remove this try/catch once the root
            // cause is found. It surfaces the real exception message in the
            // browser alert (via ForgotPasswordResponse.Message) so you don't
            // need terminal/Visual Studio output access to see what's failing.
            try
            {
                var email = model?.Email?.Trim() ?? string.Empty;

                if (string.IsNullOrEmpty(email))
                {
                    ModelState.AddModelError("Email", "Email is required");
                }
                else if (!IsValidEmail(email))
                {
                    ModelState.AddModelError("Email", "Invalid email address");
                }

                if (!ModelState.IsValid)
                {
                    var errorMessages = ModelState.Where(m => m.Value?.Errors.Count > 0)
                        .SelectMany(k => k.Value!.Errors.Select(e => e.ErrorMessage))
                        .ToList();

                    return Json(new ForgotPasswordResponse
                    {
                        Success = false,
                        Message = errorMessages.Any() ? string.Join("; ", errorMessages) : "Please enter a valid email address.",
                        Errors = ModelState.Where(m => m.Value?.Errors.Count > 0)
                            .ToDictionary(k => k.Key, v => v.Value?.Errors.Select(e => e.ErrorMessage).ToArray())
                    });
                }

                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    return Json(new ForgotPasswordResponse
                    {
                        Success = false,
                        Message = "If an account exists, a verification code has been sent."
                    });
                }

                if (!user.IsActive)
                {
                    return Json(new ForgotPasswordResponse
                    {
                        Success = false,
                        Message = "This account has been deactivated. Please contact administrator."
                    });
                }

                var rateLimitResult = await CheckRateLimitAsync(user.Id);
                if (!rateLimitResult.Success)
                {
                    return Json(new ForgotPasswordResponse
                    {
                        Success = false,
                        Message = rateLimitResult.Message,
                        ResendCooldown = rateLimitResult.CooldownSeconds
                    });
                }

                var otpCode = GenerateOtpCode();
                var expiryDate = DateTime.UtcNow.AddMinutes(_resetSettings.OtpExpiryMinutes);

                var existingToken = await _context.PasswordResetTokens
                    .FirstOrDefaultAsync(t => t.UserId == user.Id && !t.IsUsed && t.ExpiryDate > DateTime.UtcNow);

                if (existingToken != null)
                {
                    existingToken.Token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    existingToken.OtpCode = otpCode;
                    existingToken.ExpiryDate = expiryDate;
                    existingToken.OtpAttempts = 0;
                    existingToken.LastOtpSentAt = DateTime.UtcNow;
                    existingToken.CreatedAt = DateTime.UtcNow;
                    existingToken.IsUsed = false;
                    existingToken.UsedAt = null;
                }
                else
                {
                    var resetToken = new PasswordResetToken
                    {
                        UserId = user.Id,
                        Token = await _userManager.GeneratePasswordResetTokenAsync(user),
                        OtpCode = otpCode,
                        ExpiryDate = expiryDate,
                        CreatedAt = DateTime.UtcNow,
                        LastOtpSentAt = DateTime.UtcNow
                    };
                    _context.PasswordResetTokens.Add(resetToken);
                }

                await _context.SaveChangesAsync();

                var emailSent = await _emailService.SendOtpEmailAsync(user.Email!, user.FullName ?? "User", otpCode, _resetSettings.OtpExpiryMinutes);

                await _notificationService.CreateNotificationAsync(
                    "Password Reset Code",
                    $"Your password reset code is: {otpCode}. It expires in {_resetSettings.OtpExpiryMinutes} minutes.",
                    user.Id,
                    null,
                    false,
                    null,
                    "System");

                if (!emailSent)
                {
                    _logger.LogWarning("Failed to send OTP email to {Email}", user.Email);
                }

                return Json(new ForgotPasswordResponse
                {
                    Success = true,
                    Message = "Verification code sent to your email.",
                    Email = user.Email,
                    ResendCooldown = _resetSettings.ResendCooldownSeconds
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ForgotPassword failed for {Email}", model?.Email);

                // TEMP: returns the real exception to the browser for debugging.
                // Remove ex.ToString() from Message before deploying anywhere real.
                return Json(new ForgotPasswordResponse
                {
                    Success = false,
                    Message = "DEBUG: " + ex.ToString()
                });
            }
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendOtp([FromBody] ForgotPasswordViewModel model)
        {
            var email = model?.Email?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(email))
            {
                return Json(new ResendOtpResponse
                {
                    Success = false,
                    Message = "Email is required."
                });
            }

            if (!IsValidEmail(email))
            {
                return Json(new ResendOtpResponse
                {
                    Success = false,
                    Message = "Invalid email address."
                });
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return Json(new ResendOtpResponse
                {
                    Success = true,
                    Message = "If an account exists, a verification code has been sent."
                });
            }

            var existingToken = await _context.PasswordResetTokens
                .FirstOrDefaultAsync(t => t.UserId == user.Id && !t.IsUsed && t.ExpiryDate > DateTime.UtcNow);

            if (existingToken == null)
            {
                return Json(new ResendOtpResponse
                {
                    Success = false,
                    Message = "No active reset session found. Please start over."
                });
            }

            if (existingToken.LastOtpSentAt.HasValue)
            {
                var timeSinceLastSend = DateTime.UtcNow - existingToken.LastOtpSentAt.Value;
                if (timeSinceLastSend.TotalSeconds < _resetSettings.ResendCooldownSeconds)
                {
                    return Json(new ResendOtpResponse
                    {
                        Success = false,
                        Message = $"Please wait {_resetSettings.ResendCooldownSeconds - (int)timeSinceLastSend.TotalSeconds} seconds before resending.",
                        CooldownSeconds = _resetSettings.ResendCooldownSeconds - (int)timeSinceLastSend.TotalSeconds
                    });
                }
            }

            var otpCode = GenerateOtpCode();
            var expiryDate = DateTime.UtcNow.AddMinutes(_resetSettings.OtpExpiryMinutes);

            existingToken.OtpCode = otpCode;
            existingToken.ExpiryDate = expiryDate;
            existingToken.OtpAttempts = 0;
            existingToken.LastOtpSentAt = DateTime.UtcNow;
            existingToken.IsUsed = false;
            existingToken.UsedAt = null;

            await _context.SaveChangesAsync();

            var emailSent = await _emailService.SendOtpEmailAsync(user.Email!, user.FullName ?? "User", otpCode, _resetSettings.OtpExpiryMinutes);

            await _notificationService.CreateNotificationAsync(
                "Password Reset Code (Resent)",
                $"Your new password reset code is: {otpCode}. It expires in {_resetSettings.OtpExpiryMinutes} minutes.",
                user.Id,
                null,
                false,
                null,
                "System");

            if (!emailSent)
            {
                _logger.LogWarning("Failed to send OTP email to {Email}", user.Email);
            }

            return Json(new ResendOtpResponse
            {
                Success = true,
                Message = "New verification code sent to your email.",
                CooldownSeconds = _resetSettings.ResendCooldownSeconds
            });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpViewModel model)
        {
            var email = model?.Email?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(email))
            {
                return Json(new ForgotPasswordResponse
                {
                    Success = false,
                    Message = "Email is required.",
                    Errors = new Dictionary<string, string[]> { { "Email", new[] { "Email is required" } } }
                });
            }

            if (!IsValidEmail(email))
            {
                return Json(new ForgotPasswordResponse
                {
                    Success = false,
                    Message = "Invalid email address.",
                    Errors = new Dictionary<string, string[]> { { "Email", new[] { "Invalid email address" } } }
                });
            }

            if (string.IsNullOrEmpty(model?.OtpCode) || model.OtpCode.Length != 6)
            {
                return Json(new ForgotPasswordResponse
                {
                    Success = false,
                    Message = "Please enter a valid 6-digit code.",
                    Errors = new Dictionary<string, string[]> { { "OtpCode", new[] { "Verification code is required" } } }
                });
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return Json(new ForgotPasswordResponse
                {
                    Success = false,
                    Message = "Invalid verification attempt."
                });
            }

            var rateLimitResult = await CheckVerifyRateLimitAsync(user.Id);
            if (!rateLimitResult.Success)
            {
                return Json(new ForgotPasswordResponse
                {
                    Success = false,
                    Message = rateLimitResult.Message,
                    ResendCooldown = rateLimitResult.CooldownSeconds
                });
            }

            var resetToken = await _context.PasswordResetTokens
                .FirstOrDefaultAsync(t => t.UserId == user.Id && !t.IsUsed && t.ExpiryDate > DateTime.UtcNow);

            if (resetToken == null)
            {
                return Json(new ForgotPasswordResponse
                {
                    Success = false,
                    Message = "Invalid or expired session. Please request a new code."
                });
            }

            if (resetToken.OtpCode != model.OtpCode)
            {
                resetToken.OtpAttempts++;
                await _context.SaveChangesAsync();

                if (resetToken.OtpAttempts >= _resetSettings.MaxOtpAttempts)
                {
                    resetToken.IsUsed = true;
                    await _context.SaveChangesAsync();
                    return Json(new ForgotPasswordResponse
                    {
                        Success = false,
                        Message = "Too many incorrect attempts. Please request a new code."
                    });
                }

                return Json(new ForgotPasswordResponse
                {
                    Success = false,
                    Message = $"Invalid code. {_resetSettings.MaxOtpAttempts - resetToken.OtpAttempts} attempts remaining."
                });
            }

            resetToken.IsUsed = true;
            resetToken.UsedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Json(new ForgotPasswordResponse
            {
                Success = true,
                Message = "Code verified successfully.",
                Email = user.Email
            });
        }

        private async Task<(bool Success, string Message, int? CooldownSeconds)> CheckVerifyRateLimitAsync(string userId)
        {
            var fifteenMinutesAgo = DateTime.UtcNow.AddMinutes(-15);
            var recentVerifyAttempts = await _context.PasswordResetTokens
                .CountAsync(t => t.UserId == userId && t.IsUsed && t.UsedAt >= fifteenMinutesAgo);

            if (recentVerifyAttempts >= 5)
            {
                var oldestAttempt = await _context.PasswordResetTokens
                    .Where(t => t.UserId == userId && t.IsUsed && t.UsedAt >= fifteenMinutesAgo)
                    .OrderBy(t => t.UsedAt)
                    .FirstOrDefaultAsync();

                if (oldestAttempt != null && oldestAttempt.UsedAt.HasValue)
                {
                    var cooldown = (int)(oldestAttempt.UsedAt.Value.AddMinutes(15) - DateTime.UtcNow).TotalSeconds;
                    return (false, $"Too many verification attempts. Please try again in {cooldown} seconds.", cooldown);
                }
            }

            return (true, string.Empty, null);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string email)
        {
            if (string.IsNullOrEmpty(email?.Trim()))
            {
                return RedirectToAction("ForgotPassword");
            }

            var model = new ResetPasswordWithOtpViewModel { Email = email.Trim() };
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordWithOtpViewModel model)
        {
            var email = model.Email?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError("Email", "Email is required");
            }
            else if (!IsValidEmail(email))
            {
                ModelState.AddModelError("Email", "Invalid email address");
            }

            var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                         Request.Headers["Accept"].ToString().Contains("application/json");

            if (!ModelState.IsValid)
            {
                if (isAjax)
                {
                    return Json(new ForgotPasswordResponse
                    {
                        Success = false,
                        Message = "Validation failed.",
                        Errors = ModelState.Where(m => m.Value?.Errors.Count > 0)
                            .ToDictionary(k => k.Key, v => v.Value?.Errors.Select(e => e.ErrorMessage).ToArray())
                    });
                }
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                if (isAjax)
                {
                    return Json(new ForgotPasswordResponse
                    {
                        Success = false,
                        Message = "Invalid reset attempt."
                    });
                }
                ModelState.AddModelError(string.Empty, "Invalid reset attempt.");
                return View(model);
            }

            var resetToken = await _context.PasswordResetTokens
                .FirstOrDefaultAsync(t => t.UserId == user.Id && t.IsUsed && t.UsedAt != null && t.ExpiryDate > DateTime.UtcNow);

            if (resetToken == null || resetToken.OtpCode != model.OtpCode)
            {
                if (isAjax)
                {
                    return Json(new ForgotPasswordResponse
                    {
                        Success = false,
                        Message = "Invalid or expired verification code."
                    });
                }
                ModelState.AddModelError(string.Empty, "Invalid or expired verification code.");
                return View(model);
            }

            var result = await _userManager.ResetPasswordAsync(user, resetToken.Token, model.NewPassword);
            if (result.Succeeded)
            {
                await _notificationService.CreateNotificationAsync(
                    "Password Changed",
                    "Your password has been successfully reset.",
                    user.Id,
                    null,
                    false,
                    null,
                    "System");

                if (isAjax)
                {
                    return Json(new ForgotPasswordResponse
                    {
                        Success = true,
                        Message = "Your password has been reset successfully. Redirecting to login...",
                        Email = user.Email
                    });
                }

                TempData["Success"] = "Your password has been reset successfully. Please log in with your new password.";
                return RedirectToAction("Login");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            if (isAjax)
            {
                return Json(new ForgotPasswordResponse
                {
                    Success = false,
                    Message = "Failed to reset password.",
                    Errors = ModelState.Where(m => m.Value?.Errors.Count > 0)
                        .ToDictionary(k => k.Key, v => v.Value?.Errors.Select(e => e.ErrorMessage).ToArray())
                });
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> AccessDenied()
        {
            return View();
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return NotFound();
                }

                var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
                if (result.Succeeded)
                {
                    await _signInManager.RefreshSignInAsync(user);
                    TempData["Success"] = "Password changed successfully.";
                    return RedirectToAction(nameof(ChangePassword));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        private string GenerateOtpCode()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            var randomInt = BitConverter.ToUInt32(bytes, 0);
            return (randomInt % 900000 + 100000).ToString();
        }

        private static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private async Task<(bool Success, string Message, int? CooldownSeconds)> CheckRateLimitAsync(string userId)
        {
            var oneHourAgo = DateTime.UtcNow.AddHours(-1);
            var recentAttempts = await _context.PasswordResetTokens
                .CountAsync(t => t.UserId == userId && t.CreatedAt >= oneHourAgo);

            if (recentAttempts >= _resetSettings.MaxAttemptsPerHour)
            {
                var oldestAttempt = await _context.PasswordResetTokens
                    .Where(t => t.UserId == userId && t.CreatedAt >= oneHourAgo)
                    .OrderBy(t => t.CreatedAt)
                    .FirstOrDefaultAsync();

                if (oldestAttempt != null)
                {
                    var cooldown = (int)(oldestAttempt.CreatedAt.AddHours(1) - DateTime.UtcNow).TotalSeconds;
                    return (false, $"Too many attempts. Please try again in {cooldown} seconds.", cooldown);
                }
            }

            return (true, string.Empty, null);
        }
    }

    public class ChangePasswordViewModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}