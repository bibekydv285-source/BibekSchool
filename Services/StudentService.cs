using BibekSchool.Data;
using BibekSchool.Models;
using BibekSchool.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BibekSchool.Services
{
    public class StudentService : IStudentService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public StudentService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<Student?> GetStudentByUserIdAsync(string userId)
        {
            return await _context.Students
                .Include(s => s.User)
                .Include(s => s.Class)
                .FirstOrDefaultAsync(s => s.UserId == userId);
        }

        public async Task<Student?> GetStudentByIdAsync(int id)
        {
            return await _context.Students
                .Include(s => s.User)
                .Include(s => s.Class)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<StudentViewModel?> GetStudentViewModelAsync(int id)
        {
            var student = await GetStudentByIdAsync(id);
            if (student == null) return null;

            return new StudentViewModel
            {
                Id = student.Id,
                FullName = student.User.FullName,
                Email = student.User.Email ?? string.Empty,
                PhoneNumber = student.User.PhoneNumber ?? string.Empty,
                DateOfBirth = student.User.DateOfBirth,
                Gender = student.User.Gender ?? string.Empty,
                Address = student.User.Address ?? string.Empty,
                AdmissionNumber = student.AdmissionNumber,
                AdmissionDate = student.AdmissionDate,
                RollNumber = student.RollNumber ?? string.Empty,
                ClassId = student.ClassId,
                FatherName = student.FatherName ?? string.Empty,
                MotherName = student.MotherName ?? string.Empty,
                FatherPhone = student.FatherPhone ?? string.Empty,
                MotherPhone = student.MotherPhone ?? string.Empty,
                GuardianAddress = student.GuardianAddress ?? string.Empty,
                BloodGroup = student.BloodGroup ?? string.Empty,
                MedicalConditions = student.MedicalConditions ?? string.Empty,
                IsActive = student.IsActive,
                ClassName = student.Class?.Name,
                Classes = await _context.SchoolClasses.Where(c => c.IsActive).ToListAsync()
            };
        }

        public async Task<List<StudentViewModel>> GetAllStudentsAsync()
        {
            return await _context.Students
                .Include(s => s.User)
                .Include(s => s.Class)
                .Where(s => s.IsActive)
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
        }

        public async Task<List<StudentViewModel>> GetStudentsByClassAsync(int classId)
        {
            return await _context.Students
                .Include(s => s.User)
                .Where(s => s.ClassId == classId && s.IsActive)
                .Select(s => new StudentViewModel
                {
                    Id = s.Id,
                    FullName = s.User.FullName,
                    Email = s.User.Email ?? string.Empty,
                    PhoneNumber = s.User.PhoneNumber ?? string.Empty,
                    AdmissionNumber = s.AdmissionNumber,
                    RollNumber = s.RollNumber ?? string.Empty,
                    IsActive = s.IsActive
                })
                .ToListAsync();
        }

        public async Task<Student> CreateStudentAsync(StudentViewModel model, string createdBy)
        {
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                DateOfBirth = model.DateOfBirth,
                Gender = model.Gender,
                Address = model.Address,
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, "Student@123");
            if (!result.Succeeded)
            {
                throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            await _userManager.AddToRoleAsync(user, "Student");

            var student = new Student
            {
                UserId = user.Id,
                AdmissionNumber = model.AdmissionNumber,
                AdmissionDate = model.AdmissionDate,
                RollNumber = model.RollNumber,
                ClassId = model.ClassId,
                FatherName = model.FatherName,
                MotherName = model.MotherName,
                FatherPhone = model.FatherPhone,
                MotherPhone = model.MotherPhone,
                GuardianAddress = model.GuardianAddress,
                BloodGroup = model.BloodGroup,
                MedicalConditions = model.MedicalConditions,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            await LogAuditAsync(createdBy, "Create", "Student", student.Id.ToString(), null, 
                System.Text.Json.JsonSerializer.Serialize(student));

            return student;
        }

        public async Task<Student> UpdateStudentAsync(StudentViewModel model, string updatedBy)
        {
            var student = await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == model.Id);

            if (student == null)
                throw new Exception("Student not found");

            var oldValues = System.Text.Json.JsonSerializer.Serialize(student);

            student.User.FullName = model.FullName;
            student.User.Email = model.Email;
            student.User.PhoneNumber = model.PhoneNumber;
            student.User.DateOfBirth = model.DateOfBirth;
            student.User.Gender = model.Gender;
            student.User.Address = model.Address;
            student.User.UpdatedAt = DateTime.UtcNow;

            student.AdmissionNumber = model.AdmissionNumber;
            student.RollNumber = model.RollNumber;
            student.ClassId = model.ClassId;
            student.FatherName = model.FatherName;
            student.MotherName = model.MotherName;
            student.FatherPhone = model.FatherPhone;
            student.MotherPhone = model.MotherPhone;
            student.GuardianAddress = model.GuardianAddress;
            student.BloodGroup = model.BloodGroup;
            student.MedicalConditions = model.MedicalConditions;
            student.IsActive = model.IsActive;
            student.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await LogAuditAsync(updatedBy, "Update", "Student", student.Id.ToString(), oldValues,
                System.Text.Json.JsonSerializer.Serialize(student));

            return student;
        }

        public async Task<bool> DeleteStudentAsync(int id, string deletedBy)
        {
            var student = await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (student == null) return false;

            var oldValues = System.Text.Json.JsonSerializer.Serialize(student);

            student.IsActive = false;
            student.User.IsActive = false;
            student.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await LogAuditAsync(deletedBy, "Delete", "Student", student.Id.ToString(), oldValues,
                System.Text.Json.JsonSerializer.Serialize(student));

            return true;
        }

        public async Task<bool> ActivateStudentAsync(int id, string activatedBy)
        {
            var student = await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (student == null) return false;

            student.IsActive = true;
            student.User.IsActive = true;
            student.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeactivateStudentAsync(int id, string deactivatedBy)
        {
            return await DeleteStudentAsync(id, deactivatedBy);
        }

        public async Task<StudentDashboardViewModel> GetStudentDashboardAsync(string userId)
        {
            var student = await GetStudentByUserIdAsync(userId);
            if (student == null) return new StudentDashboardViewModel();

            var currentClass = student.Class;
            var subjects = new List<Subject>();
            if (currentClass != null)
            {
                subjects = await _context.ClassSubjects
                    .Where(cs => cs.ClassId == currentClass.Id && cs.IsActive)
                    .Select(cs => cs.Subject)
                    .Where(s => s.IsActive)
                    .ToListAsync();
            }

            var recentMarks = await _context.Marks
                .Where(m => m.StudentId == student.Id)
                .Include(m => m.Subject)
                .Include(m => m.Teacher)
                .ThenInclude(t => t.User)
                .OrderByDescending(m => m.CreatedAt)
                .Take(10)
                .ToListAsync();

            var notifications = await _context.Notifications
                .Where(n => n.TargetUserId == userId || (n.TargetRole == "Student" && n.IsGlobal))
                .OrderByDescending(n => n.CreatedAt)
                .Take(10)
                .ToListAsync();

            var latestResult = await _context.Results
                .Where(r => r.StudentId == student.Id)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync();

            var averagePercentage = recentMarks.Any() ? recentMarks.Average(m => m.Percentage) : 0;

            return new StudentDashboardViewModel
            {
                Student = student,
                CurrentClass = currentClass,
                Subjects = subjects,
                RecentMarks = recentMarks,
                Notifications = notifications,
                LatestResult = latestResult,
                TotalSubjects = subjects.Count,
                AveragePercentage = Math.Round(averagePercentage, 2),
                UnreadNotificationsCount = notifications.Count(n => !n.IsRead)
            };
        }

        public async Task<List<Mark>> GetStudentMarksAsync(int studentId, string? academicYear = null)
        {
            var query = _context.Marks
                .Where(m => m.StudentId == studentId);

            if (!string.IsNullOrEmpty(academicYear))
            {
                query = query.Where(m => m.AcademicYear == academicYear);
            }

            return await query
                .Include(m => m.Subject)
                .Include(m => m.Teacher)
                .ThenInclude(t => t.User)
                .OrderByDescending(m => m.ExamDate)
                .ToListAsync();
        }

        public async Task<Result?> GetStudentLatestResultAsync(int studentId)
        {
            return await _context.Results
                .Where(r => r.StudentId == studentId)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> IsStudentInClassAsync(int studentId, int classId)
        {
            return await _context.Students.AnyAsync(s => s.Id == studentId && s.ClassId == classId);
        }

        private async Task LogAuditAsync(string userId, string action, string entityType, string entityId, string? oldValues, string? newValues)
        {
            var auditLog = new AuditLog
            {
                UserId = userId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                OldValues = oldValues,
                NewValues = newValues,
                CreatedAt = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }
    }
}