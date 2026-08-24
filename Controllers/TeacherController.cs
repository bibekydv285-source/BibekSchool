using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BibekSchool.Data;
using BibekSchool.Models;
using BibekSchool.Services;
using BibekSchool.ViewModels;

namespace BibekSchool.Controllers
{
    [Authorize(Roles = "Teacher")]
    public class TeacherController : Controller
    {
        private readonly ITeacherService _teacherService;
        private readonly IMarkService _markService;
        private readonly INotificationService _notificationService;
        private readonly ApplicationDbContext _context;

        public TeacherController(
            ITeacherService teacherService,
            IMarkService markService,
            INotificationService notificationService,
            ApplicationDbContext context)
        {
            _teacherService = teacherService;
            _markService = markService;
            _notificationService = notificationService;
            _context = context;
        }

        public async Task<IActionResult> Dashboard()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Account");

            var model = await _teacherService.GetTeacherDashboardAsync(userId);
            return View(model);
        }

        public async Task<IActionResult> Classes()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Account");

            var teacher = await _teacherService.GetTeacherByUserIdAsync(userId);
            if (teacher == null) return NotFound();

            var classes = await _teacherService.GetAssignedClassesAsync(teacher.Id);
            return View(classes);
        }

        public async Task<IActionResult> Students(int classId)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Account");

            var teacher = await _teacherService.GetTeacherByUserIdAsync(userId);
            if (teacher == null) return NotFound();

            var isAssigned = await _context.TeacherAssignments
                .AnyAsync(ta => ta.TeacherId == teacher.Id && ta.ClassId == classId && ta.IsActive);

            if (!isAssigned) return Forbid();

            var students = await _context.Students
                .Where(s => s.ClassId == classId && s.IsActive)
                .Include(s => s.User)
                .ToListAsync();

            ViewBag.ClassId = classId;
            ViewBag.ClassName = (await _context.SchoolClasses.FindAsync(classId))?.Name;
            return View(students);
        }

        public async Task<IActionResult> Subjects()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Account");

            var teacher = await _teacherService.GetTeacherByUserIdAsync(userId);
            if (teacher == null) return NotFound();

            var subjects = await _teacherService.GetAssignedSubjectsAsync(teacher.Id);
            return View(subjects);
        }

        public async Task<IActionResult> Marks(int? classId, int? subjectId, string? academicYear, string? examName)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Account");

            var teacher = await _teacherService.GetTeacherByUserIdAsync(userId);
            if (teacher == null) return NotFound();

            var assignments = await _teacherService.GetAssignmentsAsync(teacher.Id);
            var assignedClassIds = assignments.Select(a => a.ClassId).Distinct().ToList();
            var assignedSubjectIds = assignments.Select(a => a.SubjectId).Distinct().ToList();

            var query = _context.Marks
                .Where(m => m.TeacherId == teacher.Id);

            if (classId.HasValue && assignedClassIds.Contains(classId.Value))
            {
                query = query.Where(m => m.Student.ClassId == classId);
            }

            if (subjectId.HasValue && assignedSubjectIds.Contains(subjectId.Value))
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
                .OrderByDescending(m => m.ExamDate)
                .ToListAsync();

            ViewBag.Classes = await _context.SchoolClasses.Where(c => assignedClassIds.Contains(c.Id)).ToListAsync();
            ViewBag.Subjects = await _context.Subjects.Where(s => assignedSubjectIds.Contains(s.Id)).ToListAsync();
            ViewBag.SelectedClassId = classId;
            ViewBag.SelectedSubjectId = subjectId;
            ViewBag.AcademicYear = academicYear ?? "2024-2025";
            ViewBag.ExamName = examName;

            return View(marks);
        }

        [HttpGet]
        public async Task<IActionResult> CreateMark(int? classId, int? subjectId)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Account");

            var teacher = await _teacherService.GetTeacherByUserIdAsync(userId);
            if (teacher == null) return NotFound();

            var assignments = await _teacherService.GetAssignmentsAsync(teacher.Id);
            var assignedClassIds = assignments.Select(a => a.ClassId).Distinct().ToList();
            var assignedSubjectIds = assignments.Select(a => a.SubjectId).Distinct().ToList();

            var model = new MarkViewModel
            {
                TeacherId = teacher.Id,
                AcademicYear = "2024-2025",
                ExamDate = DateTime.UtcNow,
                Classes = await _context.SchoolClasses.Where(c => assignedClassIds.Contains(c.Id)).ToListAsync(),
                Subjects = await _context.Subjects.Where(s => assignedSubjectIds.Contains(s.Id)).ToListAsync(),
                Teachers = new List<Teacher> { teacher }
            };

            if (classId.HasValue && assignedClassIds.Contains(classId.Value))
            {
                var students = await _context.Students
                    .Where(s => s.ClassId == classId && s.IsActive)
                    .Include(s => s.User)
                    .ToListAsync();
                model.Students = students;
                model.ClassId = classId.Value;
            }

            if (subjectId.HasValue && assignedSubjectIds.Contains(subjectId.Value))
            {
                model.SubjectId = subjectId.Value;
                var subject = await _context.Subjects.FindAsync(subjectId.Value);
                if (subject != null)
                {
                    model.FullMarks = subject.FullMarks;
                    model.PassMarks = subject.PassMarks;
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMark(MarkViewModel model)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Account");

            var teacher = await _teacherService.GetTeacherByUserIdAsync(userId);
            if (teacher == null) return NotFound();

            var isAssigned = await _teacherService.IsTeacherAssignedToClassSubjectAsync(
                teacher.Id, model.Student.ClassId ?? 0, model.SubjectId);

            if (!isAssigned) return Forbid();

            if (ModelState.IsValid)
            {
                try
                {
                    model.TeacherId = teacher.Id;
                    await _markService.CreateMarkAsync(model, userId);
                    TempData["Success"] = "Mark added successfully.";
                    return RedirectToAction(nameof(Marks));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                }
            }

            var assignments = await _teacherService.GetAssignmentsAsync(teacher.Id);
            var assignedClassIds = assignments.Select(a => a.ClassId).Distinct().ToList();
            var assignedSubjectIds = assignments.Select(a => a.SubjectId).Distinct().ToList();

            model.Classes = await _context.SchoolClasses.Where(c => assignedClassIds.Contains(c.Id)).ToListAsync();
            model.Subjects = await _context.Subjects.Where(s => assignedSubjectIds.Contains(s.Id)).ToListAsync();
            model.Teachers = new List<Teacher> { teacher };

            if (model.StudentId > 0)
            {
                var student = await _context.Students
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.Id == model.StudentId);
                if (student != null)
                {
                    model.Students = new List<Student> { student };
                }
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> EditMark(int id)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Account");

            var teacher = await _teacherService.GetTeacherByUserIdAsync(userId);
            if (teacher == null) return NotFound();

            var markViewModel = await _markService.GetMarkViewModelAsync(id);
            if (markViewModel == null) return NotFound();

            if (markViewModel.TeacherId != teacher.Id) return Forbid();

            var assignments = await _teacherService.GetAssignmentsAsync(teacher.Id);
            var assignedClassIds = assignments.Select(a => a.ClassId).Distinct().ToList();
            var assignedSubjectIds = assignments.Select(a => a.SubjectId).Distinct().ToList();

            markViewModel.Classes = await _context.SchoolClasses.Where(c => assignedClassIds.Contains(c.Id)).ToListAsync();
            markViewModel.Subjects = await _context.Subjects.Where(s => assignedSubjectIds.Contains(s.Id)).ToListAsync();
            markViewModel.Teachers = new List<Teacher> { teacher };

            return View(markViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMark(MarkViewModel model)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Account");

            var teacher = await _teacherService.GetTeacherByUserIdAsync(userId);
            if (teacher == null) return NotFound();

            var existingMark = await _markService.GetMarkByIdAsync(model.Id);
            if (existingMark == null || existingMark.TeacherId != teacher.Id) return Forbid();

            if (ModelState.IsValid)
            {
                try
                {
                    model.TeacherId = teacher.Id;
                    await _markService.UpdateMarkAsync(model, userId);
                    TempData["Success"] = "Mark updated successfully.";
                    return RedirectToAction(nameof(Marks));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                }
            }

            var assignments = await _teacherService.GetAssignmentsAsync(teacher.Id);
            var assignedClassIds = assignments.Select(a => a.ClassId).Distinct().ToList();
            var assignedSubjectIds = assignments.Select(a => a.SubjectId).Distinct().ToList();

            model.Classes = await _context.SchoolClasses.Where(c => assignedClassIds.Contains(c.Id)).ToListAsync();
            model.Subjects = await _context.Subjects.Where(s => assignedSubjectIds.Contains(s.Id)).ToListAsync();
            model.Teachers = new List<Teacher> { teacher };

            return View(model);
        }

        public async Task<IActionResult> Results()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Account");

            var teacher = await _teacherService.GetTeacherByUserIdAsync(userId);
            if (teacher == null) return NotFound();

            var assignments = await _teacherService.GetAssignmentsAsync(teacher.Id);
            var classIds = assignments.Select(a => a.ClassId).Distinct().ToList();

            var query = _context.Results
                .Where(r => classIds.Contains(r.Student.ClassId ?? 0));

            var results = await query
                .Include(r => r.Student)
                .ThenInclude(s => s.User)
                .Include(r => r.Student)
                .ThenInclude(s => s.Class)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(results);
        }

        public async Task<IActionResult> Notifications()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Account");

            var notifications = await _notificationService.GetUserNotificationsAsync(userId, "Teacher");
            return View(notifications);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkNotificationRead(int id)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            await _notificationService.MarkAsReadAsync(id, userId);
            return Ok();
        }
    }
}