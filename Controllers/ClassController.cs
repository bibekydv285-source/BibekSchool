using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BibekSchool.Data;
using BibekSchool.Models;
using BibekSchool.ViewModels;

namespace BibekSchool.Controllers
{
    [Authorize(Roles = "MainAdmin,Admin")]
    public class ClassController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ClassController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var classes = await _context.SchoolClasses
                .Include(c => c.ClassTeacher)
                .ThenInclude(t => t!.User)
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ThenBy(c => c.Section)
                .ToListAsync();

            return View(classes);
        }

        public async Task<IActionResult> Create()
        {
            var model = new ClassViewModel
            {
                Teachers = await _context.Teachers
                    .Include(t => t.User)
                    .Where(t => t.IsActive)
                    .ToListAsync(),
                Subjects = await _context.Subjects.Where(s => s.IsActive).ToListAsync(),
                AcademicYear = "2024-2025"
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ClassViewModel model)
        {
            if (ModelState.IsValid)
            {
                var schoolClass = new SchoolClass
                {
                    Name = model.Name,
                    Section = model.Section,
                    Description = model.Description,
                    ClassTeacherId = model.ClassTeacherId,
                    Capacity = model.Capacity,
                    AcademicYear = model.AcademicYear,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.SchoolClasses.Add(schoolClass);
                await _context.SaveChangesAsync();

                if (model.SelectedSubjectIds.Any())
                {
                    foreach (var subjectId in model.SelectedSubjectIds)
                    {
                        _context.ClassSubjects.Add(new ClassSubject
                        {
                            ClassId = schoolClass.Id,
                            SubjectId = subjectId,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                    await _context.SaveChangesAsync();
                }

                TempData["Success"] = "Class created successfully.";
                return RedirectToAction(nameof(Index));
            }

            model.Teachers = await _context.Teachers
                .Include(t => t.User)
                .Where(t => t.IsActive)
                .ToListAsync();
            model.Subjects = await _context.Subjects.Where(s => s.IsActive).ToListAsync();
            return View(model);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var schoolClass = await _context.SchoolClasses
                .Include(c => c.ClassSubjects)
                .ThenInclude(cs => cs.Subject)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (schoolClass == null) return NotFound();

            var model = new ClassViewModel
            {
                Id = schoolClass.Id,
                Name = schoolClass.Name,
                Section = schoolClass.Section,
                Description = schoolClass.Description,
                ClassTeacherId = schoolClass.ClassTeacherId,
                Capacity = schoolClass.Capacity,
                AcademicYear = schoolClass.AcademicYear,
                IsActive = schoolClass.IsActive,
                SelectedSubjectIds = schoolClass.ClassSubjects.Where(cs => cs.IsActive).Select(cs => cs.SubjectId).ToList(),
                Teachers = await _context.Teachers
                    .Include(t => t.User)
                    .Where(t => t.IsActive)
                    .ToListAsync(),
                Subjects = await _context.Subjects.Where(s => s.IsActive).ToListAsync()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ClassViewModel model)
        {
            if (ModelState.IsValid)
            {
                var schoolClass = await _context.SchoolClasses
                    .Include(c => c.ClassSubjects)
                    .FirstOrDefaultAsync(c => c.Id == model.Id);

                if (schoolClass == null) return NotFound();

                schoolClass.Name = model.Name;
                schoolClass.Section = model.Section;
                schoolClass.Description = model.Description;
                schoolClass.ClassTeacherId = model.ClassTeacherId;
                schoolClass.Capacity = model.Capacity;
                schoolClass.AcademicYear = model.AcademicYear;
                schoolClass.IsActive = model.IsActive;
                schoolClass.UpdatedAt = DateTime.UtcNow;

                var existingSubjects = schoolClass.ClassSubjects.Where(cs => cs.IsActive).ToList();
                foreach (var cs in existingSubjects)
                {
                    cs.IsActive = false;
                }

                foreach (var subjectId in model.SelectedSubjectIds)
                {
                    var existing = existingSubjects.FirstOrDefault(cs => cs.SubjectId == subjectId);
                    if (existing != null)
                    {
                        existing.IsActive = true;
                    }
                    else
                    {
                        _context.ClassSubjects.Add(new ClassSubject
                        {
                            ClassId = schoolClass.Id,
                            SubjectId = subjectId,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }

                await _context.SaveChangesAsync();

                TempData["Success"] = "Class updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            model.Teachers = await _context.Teachers
                .Include(t => t.User)
                .Where(t => t.IsActive)
                .ToListAsync();
            model.Subjects = await _context.Subjects.Where(s => s.IsActive).ToListAsync();
            return View(model);
        }

        public async Task<IActionResult> Details(int id)
        {
            var schoolClass = await _context.SchoolClasses
                .Include(c => c.ClassTeacher)
                .ThenInclude(t => t!.User)
                .Include(c => c.ClassSubjects)
                .ThenInclude(cs => cs.Subject)
                .Include(c => c.Students)
                .ThenInclude(s => s!.User)
                .Include(c => c.TeacherAssignments)
                .ThenInclude(ta => ta.Teacher)
                .ThenInclude(t => t!.User)
                .Include(c => c.TeacherAssignments)
                .ThenInclude(ta => ta.Subject)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (schoolClass == null) return NotFound();

            return View(schoolClass);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var schoolClass = await _context.SchoolClasses.FindAsync(id);
            if (schoolClass == null) return NotFound();

            schoolClass.IsActive = false;
            schoolClass.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Class deactivated successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}