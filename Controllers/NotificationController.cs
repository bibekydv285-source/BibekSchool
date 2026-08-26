using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BibekSchool.Services;
using System.Security.Claims;

namespace BibekSchool.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            // Use priority role logic (MainAdmin > Admin > Teacher > Student)
            var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
            var priorityRoles = new[] { "MainAdmin", "Admin", "Teacher", "Student" };
            var role = priorityRoles.FirstOrDefault(r => roles.Contains(r)) ?? "Student";

            var notifications = await _notificationService.GetUserNotificationsAsync(userId!, role);
            return View(notifications);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
            var priorityRoles = new[] { "MainAdmin", "Admin", "Teacher", "Student" };
            var role = priorityRoles.FirstOrDefault(r => roles.Contains(r)) ?? "Student";

            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            await _notificationService.MarkAsReadAsync(id, userId, role);
            return Ok();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
            var priorityRoles = new[] { "MainAdmin", "Admin", "Teacher", "Student" };
            var role = priorityRoles.FirstOrDefault(r => roles.Contains(r)) ?? "Student";

            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            await _notificationService.MarkAllAsReadAsync(userId, role);
            return Ok();
        }

[HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
            var priorityRoles = new[] { "MainAdmin", "Admin", "Teacher", "Student" };
            var role = priorityRoles.FirstOrDefault(r => roles.Contains(r)) ?? "Student";

            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            await _notificationService.DeleteNotificationAsync(id, userId, role);
            return Ok();
        }
    }
}