using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BibekSchool.Data;
using BibekSchool.Models;
using BibekSchool.ViewModels;

namespace BibekSchool.Controllers
{
    [Authorize(Roles = "MainAdmin,Admin")]
    public class SubjectController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SubjectController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var subjects = await _context.Subjects
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync();

            return View(subjects);
        }

        public async Task<IActionResult> Create()
        {
            var model = new SubjectViewModel
            {
                Classes = await _context.SchoolClasses.Where(c => c.IsActive).ToListAsync()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SubjectViewModel model)
        {
            if (ModelState.IsValid)
            {
                var subject = new Subject
                {
                    Name = model.Name,
                    Code = model.Code,
                    Description = model.Description,
                    IsCoreSubject = model.IsCoreSubject,
                    FullMarks = model.FullMarks,
                    PassMarks = model.PassMarks,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Subjects.Add(subject);
                await _context.SaveChangesAsync();

                if (model.AssignedClassIds.Any())
                {
                    foreach (var classId in model.AssignedClassIds)
                    {
                        _context.ClassSubjects.Add(new ClassSubject
                        {
                            ClassId = classId,
                            SubjectId = subject.Id,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                    await _context.SaveChangesAsync();
                }

                TempData["Success"] = "Subject created successfully.";
                return RedirectToAction(nameof(Index));
            }

            model.Classes = await _context.SchoolClasses.Where(c => c.IsActive).ToListAsync();
            return View(model);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var subject = await _context.Subjects
                .Include(s => s.ClassSubjects)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (subject == null) return NotFound();

            var model = new SubjectViewModel
            {
                Id = subject.Id,
                Name = subject.Name,
                Code = subject.Code,
                Description = subject.Description,
                IsCoreSubject = subject.IsCoreSubject,
                FullMarks = subject.FullMarks,
                PassMarks = subject.PassMarks,
                IsActive = subject.IsActive,
                AssignedClassIds = subject.ClassSubjects.Where(cs => cs.IsActive).Select(cs => cs.ClassId).ToList(),
                Classes = await _context.SchoolClasses.Where(c => c.IsActive).ToListAsync()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(SubjectViewModel model)
        {
            if (ModelState.IsValid)
            {
                var subject = await _context.Subjects
                    .Include(s => s.ClassSubjects)
                    .FirstOrDefaultAsync(s => s.Id == model.Id);

                if (subject == null) return NotFound();

                subject.Name = model.Name;
                subject.Code = model.Code;
                subject.Description = model.Description;
                subject.IsCoreSubject = model.IsCoreSubject;
                subject.FullMarks = model.FullMarks;
                subject.PassMarks = model.PassMarks;
                subject.IsActive = model.IsActive;
                subject.UpdatedAt = DateTime.UtcNow;

                var existingClassSubjects = subject.ClassSubjects.Where(cs => cs.IsActive).ToList();
                foreach (var cs in existingClassSubjects)
                {
                    cs.IsActive = false;
                }

                foreach (var classId in model.AssignedClassIds)
                {
                    var existing = existingClassSubjects.FirstOrDefault(cs => cs.ClassId == classId);
                    if (existing != null)
                    {
                        existing.IsActive = true;
                    }
                    else
                    {
                        _context.ClassSubjects.Add(new ClassSubject
                        {
                            ClassId = classId,
                            SubjectId = subject.Id,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }

                await _context.SaveChangesAsync();

                TempData["Success"] = "Subject updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            model.Classes = await _context.SchoolClasses.Where(c => c.IsActive).ToListAsync();
            return View(model);
        }

        public async Task<IActionResult> Details(int id)
        {
            var subject = await _context.Subjects
                .Include(s => s.ClassSubjects)
                .ThenInclude(cs => cs.Class)
                .Include(s => s.TeacherAssignments)
                .ThenInclude(ta => ta.Teacher)
                .ThenInclude(t => t.User)
                .Include(s => s.TeacherAssignments)
                .ThenInclude(ta => ta.Class)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (subject == null) return NotFound();

            return View(subject);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var subject = await _context.Subjects.FindAsync(id);
            if (subject == null) return NotFound();

            subject.IsActive = false;
            subject.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Subject deactivated successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}