using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BibekSchool.Data;
using BibekSchool.Models;
using BibekSchool.ViewModels;
using BibekSchool.Services;
using System.Diagnostics;

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
        private readonly IEmailService _emailService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IStudentService studentService,
            ITeacherService teacherService,
            IMarkService markService,
            IResultService resultService,
            INotificationService notificationService,
            IEmailService emailService,
            ApplicationDbContext context,
            ILogger<AdminController> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _studentService = studentService;
            _teacherService = teacherService;
            _markService = markService;
            _resultService = resultService;
            _notificationService = notificationService;
            _emailService = emailService;
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Dashboard()
        {
            try
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
                var unreadNotifications = 0;
                if (!string.IsNullOrEmpty(currentUserId))
                {
                    unreadNotifications = await _notificationService.GetUnreadCountAsync(currentUserId, "Admin");
                }

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
            catch (Exception ex)
            {
                // Log full exception chain for debugging
                var level = 0;
                var current = ex;
                while (current != null)
                {
                    _logger.LogError(ex,
                        "Admin Dashboard error — [Level {Level}] {ExType}: {Message}",
                        level, current.GetType().Name, current.Message);
                    current = current.InnerException;
                    level++;
                }

                // Log and show the dashboard with a friendly message instead of returning a raw 500.
                _logger.LogError(ex, "Failed to build Admin Dashboard for user {UserId}", _userManager.GetUserId(User));

                var fallbackModel = new AdminDashboardViewModel();
                ViewBag.ErrorMessage = "An error occurred while loading the dashboard. Please try again later.";
                return View(fallbackModel);
            }
        }

        public async Task<IActionResult> Students(int? classId, string? search)
        {
            var query = _context.Students
                .Include(s => s.User)
                .Include(s => s.Class)
                .Where(s => s.IsActive)
                .AsQueryable();

            if (classId.HasValue)
            {
                query = query.Where(s => s.ClassId == classId);
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(s => 
                    s.User.FullName.Contains(search) ||
                    s.AdmissionNumber.Contains(search) ||
                    (s.User.Email != null && s.User.Email.Contains(search)));
            }

            var students = await query
                .Select(s => new StudentViewModel
                {
                    Id = s.Id,
                    FullName = s.User.FullName,
                    Email = s.User.Email ?? string.Empty,
                    PhoneNumber = s.User.PhoneNumber ?? string.Empty,
                    AdmissionNumber = s.AdmissionNumber,
                    AdmissionDate = s.AdmissionDate,
                    RollNumber = s.RollNumber ?? string.Empty,
                    ClassId = s.ClassId,
                    ClassName = s.Class != null ? s.Class.Name + " " + s.Class.Section : string.Empty,
                    IsActive = s.IsActive
                })
                .ToListAsync();

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
                    var student = await _studentService.CreateStudentAsync(model, currentUserId);
                    
                    // Send welcome email to the student
                    var user = await _userManager.FindByIdAsync(student.UserId);
                    if (user != null)
                    {
                        var loginUrl = $"{Request.Scheme}://{Request.Host}/Account/Login";
                        await _emailService.SendRegistrationConfirmationAsync(
                            user.Email!,
                            user.FullName ?? "Student",
                            "Student",
                            loginUrl,
                            "Student@123"
                        );
                    }

                    ModelState.Clear();
                    ViewBag.SuccessMessage = "Student created successfully! A welcome email with account details has been sent to the student's email address.";
                    model = new StudentViewModel { Classes = await _context.SchoolClasses.Where(c => c.IsActive).ToListAsync() };
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                    model.Classes = await _context.SchoolClasses.Where(c => c.IsActive).ToListAsync();
                }
            }
            else
            {
                model.Classes = await _context.SchoolClasses.Where(c => c.IsActive).ToListAsync();
            }
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
            var query = _context.Teachers
                .Include(t => t.User)
                .Where(t => t.IsActive)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(t => 
                    t.User.FullName.Contains(search) ||
                    t.EmployeeId.Contains(search) ||
                    (t.User.Email != null && t.User.Email.Contains(search)));
            }

            var teachers = await query
                .Select(t => new TeacherViewModel
                {
                    Id = t.Id,
                    FullName = t.User.FullName,
                    Email = t.User.Email ?? string.Empty,
                    PhoneNumber = t.User.PhoneNumber ?? string.Empty,
                    EmployeeId = t.EmployeeId,
                    JoiningDate = t.JoiningDate,
                    Qualification = t.Qualification ?? string.Empty,
                    Designation = t.Designation ?? string.Empty,
                    IsActive = t.IsActive
                })
                .ToListAsync();

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
                    var teacher = await _teacherService.CreateTeacherAsync(model, currentUserId);
                    
                    // Send welcome email to the teacher
                    var user = await _userManager.FindByIdAsync(teacher.UserId);
                    if (user != null)
                    {
                        var loginUrl = $"{Request.Scheme}://{Request.Host}/Account/Login";
                        await _emailService.SendRegistrationConfirmationAsync(
                            user.Email!,
                            user.FullName ?? "Teacher",
                            "Teacher",
                            loginUrl,
                            model.Password
                        );
                    }

                    ModelState.Clear();
                    ViewBag.SuccessMessage = "Teacher created successfully! A welcome email with account details has been sent to the teacher's email address.";
                    model = new TeacherViewModel 
                    { 
                        Classes = await _context.SchoolClasses.Where(c => c.IsActive).ToListAsync(),
                        Subjects = await _context.Subjects.Where(s => s.IsActive).ToListAsync()
                    };
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                    model.Classes = await _context.SchoolClasses.Where(c => c.IsActive).ToListAsync();
                    model.Subjects = await _context.Subjects.Where(s => s.IsActive).ToListAsync();
                }
            }
            else
            {
                model.Classes = await _context.SchoolClasses.Where(c => c.IsActive).ToListAsync();
                model.Subjects = await _context.Subjects.Where(s => s.IsActive).ToListAsync();
            }
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
                .ThenInclude(t => t!.User)
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
            await _notificationService.MarkAsReadAsync(id, currentUserId, "Admin");
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