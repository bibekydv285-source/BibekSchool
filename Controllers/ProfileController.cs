using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using BibekSchool.Models;
using BibekSchool.ViewModels;

namespace BibekSchool.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public ProfileController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var model = new ProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                DateOfBirth = user.DateOfBirth,
                Gender = user.Gender ?? string.Empty,
                Address = user.Address ?? string.Empty,
                ProfileImage = user.ProfileImage
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ProfileViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return NotFound();

                user.FullName = model.FullName;
                user.PhoneNumber = model.PhoneNumber;
                user.DateOfBirth = model.DateOfBirth;
                user.Gender = model.Gender;
                user.Address = model.Address;
                user.UpdatedAt = DateTime.UtcNow;

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    await _signInManager.RefreshSignInAsync(user);
                    TempData["Success"] = "Profile updated successfully.";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return NotFound();

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
    }
}