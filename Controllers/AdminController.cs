using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BibekSchool.Data;
using BibekSchool.Models;
using BibekSchool.ViewModels;
using BibekSchool.Services;

namespace BibekSchool.Controllers
{
    [Authorize(Roles = "MainAdmin,Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IStudentService _studentService;
        private readonly ITeacherService _teacherService;
        private readonly IMarkService _markService;
        private readonly IResultService _resultService;
        private readonly INotificationService _notificationService;
        private readonly ApplicationDbContext _context;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IStudentService studentService,
            ITeacherService teacherService,
            IMarkService markService,
            IResultService resultService,
            INotificationService notificationService,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _studentService = studentService;
            _teacherService = teacherService;
            _markService = markService;
            _resultService = resultService;
            _notificationService = notificationService;
            _context = context;
        }

        public async Task<IActionResult> Dashboard()
        {
            var totalStudents = await _context.Students.CountAsync(s => s.IsActive);
            var totalTeachers = await _context.Teachers.CountAsync(t => t.IsActive);
            var totalClasses = await _context.SchoolClasses.CountAsync(c => c.IsActive);
            var totalSubjects = await _context.Subjects.CountAsync(s => s.IsActive);
            var activeStudents = await _context.Students.CountAsync(s => s.IsActive);
            var activeTeachers = await _context.Teachers.CountAsync(t => t.IsActive);

            var recentRegistrations = await _context.Users
                .Where(u => u.IsActive)
                .OrderByDescending(u => u.CreatedAt)
                .Take(10)
                .ToListAsync();

            var recentResults = await _context.Results
                .Include(r => r.Student)
                .ThenInclude(s => s.User)
                .OrderByDescending(r => r.CreatedAt)
                .Take(10)
                .ToListAsync();

            var recentActivities = await _context.AuditLogs
                .Include(a => a.User)
                .OrderByDescending(a => a.CreatedAt)
                .Take(20)
                .ToListAsync();

            var currentUserId = _userManager.GetUserId(User);
            var unreadNotifications = await _notificationService.GetUnreadCountAsync(currentUserId!, "Admin");

            var model = new AdminDashboardViewModel
            {
                TotalStudents = totalStudents,
                TotalTeachers = totalTeachers,
                TotalClasses = totalClasses,
                TotalSubjects = totalSubjects,
                ActiveStudents = activeStudents,
                ActiveTeachers = activeTeachers,
                RecentRegistrations = recentRegistrations,
                RecentResults = recentResults,
                RecentActivities = recentActivities,
                UnreadNotificationsCount = unreadNotifications
            };

            return View(model);
        }

        public async Task<IActionResult> Students(int? classId, string? search)
        {
            var students = await _studentService.GetAllStudentsAsync();

            if (classId.HasValue)
            {
                students = students.Where(s => s.ClassId == classId).ToList();
            }

            if (!string.IsNullOrEmpty(search))
            {
                students = students.Where(s => 
                    s.FullName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    s.AdmissionNumber.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    s.Email.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            ViewBag.Classes = await _context.SchoolClasses.Where(c => c.IsActive).ToListAsync();
            ViewBag.SelectedClassId = classId;
            ViewBag.Search = search;

            return View(students);
        }

        [HttpGet]
        public async Task<IActionResult> CreateStudent()
        {
            var model = new StudentViewModel
            {
                Classes = await _context.SchoolClasses.Where(c => c.IsActive).ToListAsync()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStudent(StudentViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var currentUserId = _userManager.GetUserId(User)!;
                    await _studentService.CreateStudentAsync(model, currentUserId);
                    TempData["Success"] = "Student created successfully.";
                    return RedirectToAction(nameof(Students));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                }
            }

            model.Classes = await _context.SchoolClasses.Where(c => c.IsActive).ToListAsync();
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> EditStudent(int id)
        {
            var model = await _studentService.GetStudentViewModelAsync(id);
            if (model == null) return NotFound();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditStudent(StudentViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var currentUserId = _userManager.GetUserId(User)!;
                    await _studentService.UpdateStudentAsync(model, currentUserId);
                    TempData["Success"] = "Student updated successfully.";
                    return RedirectToAction(nameof(Students));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                }
            }

            model.Classes = await _context.SchoolClasses.Where(c => c.IsActive).ToListAsync();
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> DeleteStudent(int id)
        {
            var student = await _studentService.GetStudentViewModelAsync(id);
            if (student == null) return NotFound();
            return View(student);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteStudentConfirmed(int id)
        {
            var currentUserId = _userManager.GetUserId(User)!;
            await _studentService.DeleteStudentAsync(id, currentUserId);
            TempData["Success"] = "Student deactivated successfully.";
            return RedirectToAction(nameof(Students));
        }

        public async Task<IActionResult> Teachers(string? search)
        {
            var teachers = await _teacherService.GetAllTeachersAsync();

            if (!string.IsNullOrEmpty(search))
            {
                teachers = teachers.Where(t => 
                    t.FullName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    t.EmployeeId.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    t.Email.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            ViewBag.Search = search;
            return View(teachers);
        }

        [HttpGet]
        public async Task<IActionResult> CreateTeacher()
        {
            var model = new TeacherViewModel
            {
                Classes = await _context.SchoolClasses.Where(c => c.IsActive).ToListAsync(),
                Subjects = await _context.Subjects.Where(s => s.IsActive).ToListAsync()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTeacher(TeacherViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var currentUserId = _userManager.GetUserId(User)!;
                    await _teacherService.CreateTeacherAsync(model, currentUserId);
                    TempData["Success"] = "Teacher created successfully.";
                    return RedirectToAction(nameof(Teachers));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                }
            }

            model.Classes = await _context.SchoolClasses.Where(c => c.IsActive).ToListAsync();
            model.Subjects = await _context.Subjects.Where(s => s.IsActive).ToListAsync();
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> EditTeacher(int id)
        {
            var model = await _teacherService.GetTeacherViewModelAsync(id);
            if (model == null) return NotFound();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTeacher(TeacherViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var currentUserId = _userManager.GetUserId(User)!;
                    await _teacherService.UpdateTeacherAsync(model, currentUserId);
                    TempData["Success"] = "Teacher updated successfully.";
                    return RedirectToAction(nameof(Teachers));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                }
            }

            model.Classes = await _context.SchoolClasses.Where(c => c.IsActive).ToListAsync();
            model.Subjects = await _context.Subjects.Where(s => s.IsActive).ToListAsync();
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> DeleteTeacher(int id)
        {
            var teacher = await _teacherService.GetTeacherViewModelAsync(id);
            if (teacher == null) return NotFound();
            return View(teacher);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTeacherConfirmed(int id)
        {
            var currentUserId = _userManager.GetUserId(User)!;
            await _teacherService.DeleteTeacherAsync(id, currentUserId);
            TempData["Success"] = "Teacher deactivated successfully.";
            return RedirectToAction(nameof(Teachers));
        }

        public async Task<IActionResult> Classes()
        {
            var classes = await _context.SchoolClasses
                .Include(c => c.ClassTeacher)
                .ThenInclude(t => t.User)
                .Where(c => c.IsActive)
                .ToListAsync();

            return View(classes);
        }

        public async Task<IActionResult> Subjects()
        {
            var subjects = await _context.Subjects.Where(s => s.IsActive).ToListAsync();
            return View(subjects);
        }

        public async Task<IActionResult> Marks(int? classId, int? subjectId, string? academicYear, string? examName)
        {
            var query = _context.Marks.AsQueryable();

            if (classId.HasValue)
            {
                query = query.Where(m => m.Student.ClassId == classId);
            }

            if (subjectId.HasValue)
            {
                query = query.Where(m => m.SubjectId == subjectId);
            }

            if (!string.IsNullOrEmpty(academicYear))
            {
                query = query.Where(m => m.AcademicYear == academicYear);
            }

            if (!string.IsNullOrEmpty(examName))
            {
                query = query.Where(m => m.ExamName == examName);
            }

            var marks = await query
                .Include(m => m.Student)
                .ThenInclude(s => s.User)
                .Include(m => m.Subject)
                .Include(m => m.Teacher)
                .ThenInclude(t => t.User)
                .OrderByDescending(m => m.CreatedAt)
                .Take(100)
                .ToListAsync();

            ViewBag.Classes = await _context.SchoolClasses.Where(c => c.IsActive).ToListAsync();
            ViewBag.Subjects = await _context.Subjects.Where(s => s.IsActive).ToListAsync();
            ViewBag.SelectedClassId = classId;
            ViewBag.SelectedSubjectId = subjectId;
            ViewBag.AcademicYear = academicYear ?? "2024-2025";
            ViewBag.ExamName = examName;

            return View(marks);
        }

        public async Task<IActionResult> Results()
        {
            var results = await _context.Results
                .Include(r => r.Student)
                .ThenInclude(s => s.User)
                .Include(r => r.Student)
                .ThenInclude(s => s.Class)
                .OrderByDescending(r => r.CreatedAt)
                .Take(100)
                .ToListAsync();

            return View(results);
        }

        public async Task<IActionResult> Users()
        {
            var users = await _context.Users
                .Where(u => u.IsActive)
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            return View(users);
        }

        public async Task<IActionResult> Notifications()
        {
            var currentUserId = _userManager.GetUserId(User)!;
            var notifications = await _notificationService.GetUserNotificationsAsync(currentUserId, "Admin");
            return View(notifications);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkNotificationRead(int id)
        {
            var currentUserId = _userManager.GetUserId(User)!;
            await _notificationService.MarkAsReadAsync(id, currentUserId);
            return Ok();
        }

        public async Task<IActionResult> AuditLogs()
        {
            var logs = await _context.AuditLogs
                .Include(a => a.User)
                .OrderByDescending(a => a.CreatedAt)
                .Take(100)
                .ToListAsync();

            return View(logs);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            TempData["Success"] = "User deactivated successfully.";
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActivateUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            user.IsActive = true;
            user.UpdatedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            TempData["Success"] = "User activated successfully.";
            return RedirectToAction(nameof(Users));
        }

        public IActionResult Settings()
        {
            return View();
        }
    }
}