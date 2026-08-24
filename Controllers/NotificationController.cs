using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BibekSchool.Services;

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
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            var notifications = await _notificationService.GetUserNotificationsAsync(userId!, role);
            return View(notifications);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            await _notificationService.MarkAsReadAsync(id, userId);
            return Ok();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            await _notificationService.MarkAllAsReadAsync(userId, role);
            return Ok();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            await _notificationService.DeleteNotificationAsync(id, userId);
            return Ok();
        }
    }
}