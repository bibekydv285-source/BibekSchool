using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BibekSchool.Data;
using BibekSchool.Models;
using BibekSchool.Services;
using BibekSchool.ViewModels;

namespace BibekSchool.Controllers
{
    [Authorize(Roles = "MainAdmin,Admin,Teacher,Student")]
    public class ResultController : Controller
    {
        private readonly IResultService _resultService;
        private readonly IStudentService _studentService;
        private readonly ITeacherService _teacherService;
        private readonly ApplicationDbContext _context;

        public ResultController(
            IResultService resultService,
            IStudentService studentService,
            ITeacherService teacherService,
            ApplicationDbContext context)
        {
            _resultService = resultService;
            _studentService = studentService;
            _teacherService = teacherService;
            _context = context;
        }

        public async Task<IActionResult> Index(string? academicYear, string? term)
        {
            var isAdmin = User.IsInRole("MainAdmin") || User.IsInRole("Admin");
            var isTeacher = User.IsInRole("Teacher");
            var isStudent = User.IsInRole("Student");
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            List<ResultViewModel> results = new();

            if (isStudent && !string.IsNullOrEmpty(userId))
            {
                var student = await _studentService.GetStudentByUserIdAsync(userId);
                if (student != null)
                {
                    results = await _resultService.GetResultsByStudentAsync(student.Id);
                }
            }
            else if (isTeacher && !string.IsNullOrEmpty(userId))
            {
                var teacher = await _teacherService.GetTeacherByUserIdAsync(userId);
                if (teacher != null)
                {
                    var assignments = await _teacherService.GetAssignmentsAsync(teacher.Id);
                    var classIds = assignments.Select(a => a.ClassId).Distinct().ToList();

                    var query = _context.Results
                        .Where(r => classIds.Contains(r.Student.ClassId ?? 0));

                    if (!string.IsNullOrEmpty(academicYear))
                        query = query.Where(r => r.AcademicYear == academicYear);
                    if (!string.IsNullOrEmpty(term))
                        query = query.Where(r => r.Term == term);

                    results = await query
                        .Include(r => r.Student)
                        .ThenInclude(s => s.User)
                        .Include(r => r.Student)
                        .ThenInclude(s => s.Class)
                        .OrderByDescending(r => r.CreatedAt)
                        .Select(r => new ResultViewModel
                        {
                            Id = r.Id,
                            StudentId = r.StudentId,
                            AcademicYear = r.AcademicYear,
                            Term = r.Term,
                            TotalObtainedMarks = r.TotalObtainedMarks,
                            TotalFullMarks = r.TotalFullMarks,
                            Percentage = r.Percentage,
                            OverallGrade = r.OverallGrade,
                            RankInClass = r.RankInClass,
                            TotalStudentsInClass = r.TotalStudentsInClass,
                            IsPassed = r.IsPassed,
                            PublishedDate = r.PublishedDate,
                            StudentName = r.Student!.User!.FullName,
                            ClassName = r.Student.Class!.Name
                        })
                        .ToListAsync();
                }
            }
            else if (isAdmin)
            {
                IQueryable<Result> query = _context.Results;

                if (!string.IsNullOrEmpty(academicYear))
                    query = query.Where(r => r.AcademicYear == academicYear);
                if (!string.IsNullOrEmpty(term))
                    query = query.Where(r => r.Term == term);

                results = await query
                    .Include(r => r.Student)
                    .ThenInclude(s => s.User)
                    .Include(r => r.Student)
                    .ThenInclude(s => s.Class)
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => new ResultViewModel
                    {
                        Id = r.Id,
                        StudentId = r.StudentId,
                        AcademicYear = r.AcademicYear,
                        Term = r.Term,
                        TotalObtainedMarks = r.TotalObtainedMarks,
                        TotalFullMarks = r.TotalFullMarks,
                        Percentage = r.Percentage,
                        OverallGrade = r.OverallGrade,
                        RankInClass = r.RankInClass,
                        TotalStudentsInClass = r.TotalStudentsInClass,
                        IsPassed = r.IsPassed,
                        PublishedDate = r.PublishedDate,
                        StudentName = r.Student!.User!.FullName,
                        ClassName = r.Student.Class!.Name
                    })
                    .ToListAsync();
            }

            ViewBag.AcademicYear = academicYear ?? "2024-2025";
            ViewBag.Term = term ?? "First Term";
            ViewBag.Terms = new List<string> { "First Term", "Second Term", "Third Term", "Final Exam" };

            return View(results);
        }

        public async Task<IActionResult> Details(int id)
        {
            var isAdmin = User.IsInRole("MainAdmin") || User.IsInRole("Admin");
            var isTeacher = User.IsInRole("Teacher");
            var isStudent = User.IsInRole("Student");
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            var resultViewModel = await _resultService.GetResultViewModelAsync(id);
            if (resultViewModel == null) return NotFound();

            if (isStudent && !string.IsNullOrEmpty(userId))
            {
                var student = await _studentService.GetStudentByUserIdAsync(userId);
                if (student == null || resultViewModel.StudentId != student.Id) return Forbid();
            }
            else if (isTeacher && !string.IsNullOrEmpty(userId))
            {
                var teacher = await _teacherService.GetTeacherByUserIdAsync(userId);
                if (teacher != null)
                {
                    var assignments = await _teacherService.GetAssignmentsAsync(teacher.Id);
                    var classIds = assignments.Select(a => a.ClassId).Distinct().ToList();

                    var student = await _context.Students.FindAsync(resultViewModel.StudentId);
                    if (student == null || !classIds.Contains(student.ClassId ?? 0)) return Forbid();
                }
            }

            return View(resultViewModel);
        }

        [Authorize(Roles = "MainAdmin,Admin")]
        [HttpGet]
        public async Task<IActionResult> Generate(int? studentId, string? academicYear, string? term)
        {
            ViewBag.Students = await _context.Students
                .Include(s => s.User)
                .Where(s => s.IsActive)
                .ToListAsync();
            ViewBag.AcademicYear = academicYear ?? "2024-2025";
            ViewBag.Term = term ?? "First Term";
            ViewBag.Terms = new List<string> { "First Term", "Second Term", "Third Term", "Final Exam" };

            return View();
        }

        [Authorize(Roles = "MainAdmin,Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate(int studentId, string academicYear, string term)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            try
            {
                var result = await _resultService.GenerateResultAsync(studentId, academicYear, term, userId!);
                TempData["Success"] = "Result generated successfully.";
                return RedirectToAction(nameof(Details), new { id = result.Id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                ViewBag.Students = await _context.Students
                    .Include(s => s.User)
                    .Where(s => s.IsActive)
                    .ToListAsync();
                ViewBag.AcademicYear = academicYear;
                ViewBag.Term = term;
                ViewBag.Terms = new List<string> { "First Term", "Second Term", "Third Term", "Final Exam" };
                return View();
            }
        }

        [Authorize(Roles = "MainAdmin,Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Publish(int id)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            try
            {
                await _resultService.PublishResultAsync(id, userId!);
                TempData["Success"] = "Result published successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id });
        }
    }
}