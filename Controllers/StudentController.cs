using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BibekSchool.Models;
using Microsoft.Extensions.Logging;
using BibekSchool.Services;
using BibekSchool.ViewModels;

namespace BibekSchool.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentController : Controller
    {
        private readonly IStudentService _studentService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<StudentController> _logger;

        public StudentController(IStudentService studentService, INotificationService notificationService, ILogger<StudentController> logger)
        {
            _studentService = studentService;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<IActionResult> Dashboard()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Account");

            var student = await _studentService.GetStudentByUserIdAsync(userId);
            if (student == null)
            {
                // User has Student role but no Student profile - redirect to AccessDenied
                return RedirectToAction("AccessDenied", "Account");
            }

            var model = await _studentService.GetStudentDashboardAsync(userId);
            return View(model);
        }

        public async Task<IActionResult> Profile()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Account");

            var student = await _studentService.GetStudentByUserIdAsync(userId);
            if (student == null) return NotFound();

            var model = await _studentService.GetStudentViewModelAsync(student.Id);
            return View(model);
        }

        public async Task<IActionResult> Class()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Account");

            var student = await _studentService.GetStudentByUserIdAsync(userId);
            if (student?.Class == null)
            {
                return View("Class", (SchoolClass?)null);
            }

            return View(student.Class);
        }

        public async Task<IActionResult> Subjects()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Account");

            var student = await _studentService.GetStudentByUserIdAsync(userId);
            if (student?.Class == null) return View(new List<Subject>());

            var subjects = await _studentService.GetStudentDashboardAsync(userId);
            return View(subjects.Subjects);
        }

        public async Task<IActionResult> Marks(string? academicYear)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Account");

            var student = await _studentService.GetStudentByUserIdAsync(userId);
            if (student == null) return NotFound();

            var marks = await _studentService.GetStudentMarksAsync(student.Id, academicYear);
            ViewBag.AcademicYear = academicYear ?? "2024-2025";
            return View(marks);
        }

        public async Task<IActionResult> Results()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Account");

            var student = await _studentService.GetStudentByUserIdAsync(userId);
            if (student == null) return NotFound();

            var results = await _studentService.GetStudentDashboardAsync(userId);
            return View(results.LatestResult);
        }

        public async Task<IActionResult> Notifications()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Account");

            var notifications = await _notificationService.GetUserNotificationsAsync(userId, "Student");
            return View(notifications);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkNotificationRead(int id)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            await _notificationService.MarkAsReadAsync(id, userId, "Student");
            return Ok();
        }
    }
}