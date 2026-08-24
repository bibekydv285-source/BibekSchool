using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BibekSchool.Data;
using BibekSchool.Models;
using BibekSchool.Services;
using BibekSchool.ViewModels;

namespace BibekSchool.Controllers
{
    [Authorize(Roles = "MainAdmin,Admin,Teacher")]
    public class MarkController : Controller
    {
        private readonly IMarkService _markService;
        private readonly ITeacherService _teacherService;
        private readonly ApplicationDbContext _context;

        public MarkController(IMarkService markService, ITeacherService teacherService, ApplicationDbContext context)
        {
            _markService = markService;
            _teacherService = teacherService;
            _context = context;
        }

        public async Task<IActionResult> Index(int? classId, int? subjectId, string? academicYear, string? examName)
        {
            var isAdmin = User.IsInRole("MainAdmin") || User.IsInRole("Admin");
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            List<MarkViewModel> marks;

            if (isAdmin)
            {
                IQueryable<Mark> query = _context.Marks;

                if (classId.HasValue)
                    query = query.Where(m => m.Student.ClassId == classId);
                if (subjectId.HasValue)
                    query = query.Where(m => m.SubjectId == subjectId);
                if (!string.IsNullOrEmpty(academicYear))
                    query = query.Where(m => m.AcademicYear == academicYear);
                if (!string.IsNullOrEmpty(examName))
                    query = query.Where(m => m.ExamName == examName);

                marks = await query
                    .Include(m => m.Student)
                    .ThenInclude(s => s.User)
                    .Include(m => m.Subject)
                    .Include(m => m.Teacher)
                    .ThenInclude(t => t.User)
                    .OrderByDescending(m => m.ExamDate)
                    .Select(m => new MarkViewModel
                    {
                        Id = m.Id,
                        StudentId = m.StudentId,
                        SubjectId = m.SubjectId,
                        TeacherId = m.TeacherId,
                        ExamName = m.ExamName,
                        AcademicYear = m.AcademicYear,
                        FullMarks = m.FullMarks,
                        PassMarks = m.PassMarks,
                        ObtainedMarks = m.ObtainedMarks,
                        Grade = m.Grade,
                        Remarks = m.Remarks,
                        ExamDate = m.ExamDate,
                        StudentName = m.Student.User.FullName,
                        SubjectName = m.Subject.Name,
                        TeacherName = m.Teacher.User.FullName
                    })
                    .ToListAsync();
            }
            else
            {
                var teacher = await _teacherService.GetTeacherByUserIdAsync(userId!);
                if (teacher == null) return NotFound();

                marks = await _markService.GetMarksByTeacherAsync(teacher.Id, academicYear);
            }

            ViewBag.Classes = await _context.SchoolClasses.Where(c => c.IsActive).ToListAsync();
            ViewBag.Subjects = await _context.Subjects.Where(s => s.IsActive).ToListAsync();
            ViewBag.SelectedClassId = classId;
            ViewBag.SelectedSubjectId = subjectId;
            ViewBag.AcademicYear = academicYear ?? "2024-2025";
            ViewBag.ExamName = examName;

            return View(marks);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var isAdmin = User.IsInRole("MainAdmin") || User.IsInRole("Admin");
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            var model = new MarkViewModel
            {
                AcademicYear = "2024-2025",
                ExamDate = DateTime.UtcNow
            };

            if (isAdmin)
            {
                model.Students = await _context.Students.Where(s => s.IsActive).Include(s => s.User).ToListAsync();
                model.Subjects = await _context.Subjects.Where(s => s.IsActive).ToListAsync();
                model.Teachers = await _context.Teachers.Where(t => t.IsActive).Include(t => t.User).ToListAsync();
            }
            else
            {
                var teacher = await _teacherService.GetTeacherByUserIdAsync(userId!);
                if (teacher == null) return NotFound();

                var assignments = await _teacherService.GetAssignmentsAsync(teacher.Id);
                var classIds = assignments.Select(a => a.ClassId).Distinct().ToList();
                var subjectIds = assignments.Select(a => a.SubjectId).Distinct().ToList();

                model.Students = await _context.Students
                    .Where(s => classIds.Contains(s.ClassId ?? 0) && s.IsActive)
                    .Include(s => s.User)
                    .ToListAsync();
                model.Subjects = await _context.Subjects.Where(s => subjectIds.Contains(s.Id)).ToListAsync();
                model.Teachers = new List<Teacher> { teacher };
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MarkViewModel model)
        {
            var isAdmin = User.IsInRole("MainAdmin") || User.IsInRole("Admin");
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (!isAdmin)
            {
                var teacher = await _teacherService.GetTeacherByUserIdAsync(userId!);
                if (teacher == null) return NotFound();

                var isAssigned = await _teacherService.IsTeacherAssignedToClassSubjectAsync(
                    teacher.Id, model.Student.ClassId ?? 0, model.SubjectId);

                if (!isAssigned) return Forbid();

                model.TeacherId = teacher.Id;
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _markService.CreateMarkAsync(model, userId!);
                    TempData["Success"] = "Mark added successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                }
            }

            if (isAdmin)
            {
                model.Students = await _context.Students.Where(s => s.IsActive).Include(s => s.User).ToListAsync();
                model.Subjects = await _context.Subjects.Where(s => s.IsActive).ToListAsync();
                model.Teachers = await _context.Teachers.Where(t => t.IsActive).Include(t => t.User).ToListAsync();
            }
            else
            {
                var teacher = await _teacherService.GetTeacherByUserIdAsync(userId!);
                var assignments = await _teacherService.GetAssignmentsAsync(teacher!.Id);
                var classIds = assignments.Select(a => a.ClassId).Distinct().ToList();
                var subjectIds = assignments.Select(a => a.SubjectId).Distinct().ToList();

                model.Students = await _context.Students
                    .Where(s => classIds.Contains(s.ClassId ?? 0) && s.IsActive)
                    .Include(s => s.User)
                    .ToListAsync();
                model.Subjects = await _context.Subjects.Where(s => subjectIds.Contains(s.Id)).ToListAsync();
                model.Teachers = new List<Teacher> { teacher };
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var isAdmin = User.IsInRole("MainAdmin") || User.IsInRole("Admin");
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            var markViewModel = await _markService.GetMarkViewModelAsync(id);
            if (markViewModel == null) return NotFound();

            if (!isAdmin)
            {
                var teacher = await _teacherService.GetTeacherByUserIdAsync(userId!);
                if (teacher == null || markViewModel.TeacherId != teacher.Id) return Forbid();
            }

            if (isAdmin)
            {
                markViewModel.Students = await _context.Students.Where(s => s.IsActive).Include(s => s.User).ToListAsync();
                markViewModel.Subjects = await _context.Subjects.Where(s => s.IsActive).ToListAsync();
                markViewModel.Teachers = await _context.Teachers.Where(t => t.IsActive).Include(t => t.User).ToListAsync();
            }
            else
            {
                var teacher = await _teacherService.GetTeacherByUserIdAsync(userId!);
                var assignments = await _teacherService.GetAssignmentsAsync(teacher!.Id);
                var classIds = assignments.Select(a => a.ClassId).Distinct().ToList();
                var subjectIds = assignments.Select(a => a.SubjectId).Distinct().ToList();

                markViewModel.Students = await _context.Students
                    .Where(s => classIds.Contains(s.ClassId ?? 0) && s.IsActive)
                    .Include(s => s.User)
                    .ToListAsync();
                markViewModel.Subjects = await _context.Subjects.Where(s => subjectIds.Contains(s.Id)).ToListAsync();
                markViewModel.Teachers = new List<Teacher> { teacher };
            }

            return View(markViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(MarkViewModel model)
        {
            var isAdmin = User.IsInRole("MainAdmin") || User.IsInRole("Admin");
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (!isAdmin)
            {
                var teacher = await _teacherService.GetTeacherByUserIdAsync(userId!);
                if (teacher == null) return NotFound();

                var existingMark = await _markService.GetMarkByIdAsync(model.Id);
                if (existingMark == null || existingMark.TeacherId != teacher.Id) return Forbid();

                model.TeacherId = teacher.Id;
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _markService.UpdateMarkAsync(model, userId!);
                    TempData["Success"] = "Mark updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                }
            }

            if (isAdmin)
            {
                model.Students = await _context.Students.Where(s => s.IsActive).Include(s => s.User).ToListAsync();
                model.Subjects = await _context.Subjects.Where(s => s.IsActive).ToListAsync();
                model.Teachers = await _context.Teachers.Where(t => t.IsActive).Include(t => t.User).ToListAsync();
            }
            else
            {
                var teacher = await _teacherService.GetTeacherByUserIdAsync(userId!);
                var assignments = await _teacherService.GetAssignmentsAsync(teacher!.Id);
                var classIds = assignments.Select(a => a.ClassId).Distinct().ToList();
                var subjectIds = assignments.Select(a => a.SubjectId).Distinct().ToList();

                model.Students = await _context.Students
                    .Where(s => classIds.Contains(s.ClassId ?? 0) && s.IsActive)
                    .Include(s => s.User)
                    .ToListAsync();
                model.Subjects = await _context.Subjects.Where(s => subjectIds.Contains(s.Id)).ToListAsync();
                model.Teachers = new List<Teacher> { teacher };
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var isAdmin = User.IsInRole("MainAdmin") || User.IsInRole("Admin");
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (!isAdmin)
            {
                var teacher = await _teacherService.GetTeacherByUserIdAsync(userId!);
                if (teacher == null) return NotFound();

                var existingMark = await _markService.GetMarkByIdAsync(id);
                if (existingMark == null || existingMark.TeacherId != teacher.Id) return Forbid();
            }

            await _markService.DeleteMarkAsync(id, userId!);
            TempData["Success"] = "Mark deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}