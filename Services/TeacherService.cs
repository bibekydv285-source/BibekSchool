using BibekSchool.Data;
using BibekSchool.Models;
using BibekSchool.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BibekSchool.Services
{
    public class TeacherService : BaseService, ITeacherService
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public TeacherService(ApplicationDbContext context, UserManager<ApplicationUser> userManager) : base(context)
        {
            _userManager = userManager;
        }

        public async Task<Teacher?> GetTeacherByUserIdAsync(string userId)
        {
            return await _context.Teachers
                .Include(t => t.User)
                .Include(t => t.Assignments)
                .ThenInclude(a => a.Class)
                .Include(t => t.Assignments)
                .ThenInclude(a => a.Subject)
                .FirstOrDefaultAsync(t => t.UserId == userId);
        }

        public async Task<Teacher?> GetTeacherByIdAsync(int id)
        {
            return await _context.Teachers
                .Include(t => t.User)
                .Include(t => t.Assignments)
                .ThenInclude(a => a.Class)
                .Include(t => t.Assignments)
                .ThenInclude(a => a.Subject)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<TeacherViewModel?> GetTeacherViewModelAsync(int id)
        {
            var teacher = await GetTeacherByIdAsync(id);
            if (teacher == null) return null;

            var assignments = await _context.TeacherAssignments
                .Where(ta => ta.TeacherId == id && ta.IsActive)
                .Include(ta => ta.Class)
                .Include(ta => ta.Subject)
                .ToListAsync();

            return new TeacherViewModel
            {
                Id = teacher.Id,
                FullName = teacher.User.FullName,
                Email = teacher.User.Email ?? string.Empty,
                PhoneNumber = teacher.User.PhoneNumber ?? string.Empty,
                DateOfBirth = teacher.User.DateOfBirth,
                Gender = teacher.User.Gender ?? string.Empty,
                Address = teacher.User.Address ?? string.Empty,
                EmployeeId = teacher.EmployeeId,
                JoiningDate = teacher.JoiningDate,
                Qualification = teacher.Qualification ?? string.Empty,
                Specialization = teacher.Specialization ?? string.Empty,
                ExperienceYears = teacher.ExperienceYears,
                Salary = teacher.Salary,
                Designation = teacher.Designation ?? string.Empty,
                EmergencyContact = teacher.EmergencyContact ?? string.Empty,
                IsActive = teacher.IsActive,
                AssignedClassIds = assignments.Select(a => a.ClassId).Distinct().ToList(),
                AssignedSubjectIds = assignments.Select(a => a.SubjectId).Distinct().ToList(),
                Classes = await _context.SchoolClasses.Where(c => c.IsActive).ToListAsync(),
                Subjects = await _context.Subjects.Where(s => s.IsActive).ToListAsync()
            };
        }

        public async Task<List<TeacherViewModel>> GetAllTeachersAsync()
        {
            return await _context.Teachers
                .Include(t => t.User)
                .Where(t => t.IsActive)
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
        }

        public async Task<Teacher> CreateTeacherAsync(TeacherViewModel model, string createdBy)
        {
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                throw new Exception("Email already registered.");
            }

            existingUser = await _userManager.FindByNameAsync(model.Email);
            if (existingUser != null)
            {
                throw new Exception("Username already taken.");
            }

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

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            await _userManager.AddToRoleAsync(user, "Teacher");

            var teacher = new Teacher
            {
                UserId = user.Id,
                EmployeeId = model.EmployeeId,
                JoiningDate = model.JoiningDate,
                Qualification = model.Qualification,
                Specialization = model.Specialization,
                ExperienceYears = model.ExperienceYears,
                Salary = model.Salary,
                Designation = model.Designation,
                EmergencyContact = model.EmergencyContact,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Teachers.Add(teacher);
            await _context.SaveChangesAsync();

            if (model.AssignedClassIds.Any() && model.AssignedSubjectIds.Any())
            {
                var academicYear = "2024-2025";
                foreach (var classId in model.AssignedClassIds)
                {
                    foreach (var subjectId in model.AssignedSubjectIds)
                    {
                        var assignment = new TeacherAssignment
                        {
                            TeacherId = teacher.Id,
                            ClassId = classId,
                            SubjectId = subjectId,
                            AcademicYear = academicYear,
                            IsActive = true,
                            AssignedDate = DateTime.UtcNow,
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.TeacherAssignments.Add(assignment);
                    }
                }
                await _context.SaveChangesAsync();
            }

            await LogAuditAsync(createdBy, "Create", "Teacher", teacher.Id.ToString(), null,
                System.Text.Json.JsonSerializer.Serialize(teacher));

            return teacher;
        }

        public async Task<Teacher> UpdateTeacherAsync(TeacherViewModel model, string updatedBy)
        {
            var teacher = await _context.Teachers
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Id == model.Id);

            if (teacher == null)
                throw new Exception("Teacher not found");

            var oldValues = System.Text.Json.JsonSerializer.Serialize(teacher);

            teacher.User.FullName = model.FullName;
            teacher.User.Email = model.Email;
            teacher.User.PhoneNumber = model.PhoneNumber;
            teacher.User.DateOfBirth = model.DateOfBirth;
            teacher.User.Gender = model.Gender;
            teacher.User.Address = model.Address;
            teacher.User.UpdatedAt = DateTime.UtcNow;

            // Update password if provided
            if (!string.IsNullOrEmpty(model.Password))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(teacher.User);
                var result = await _userManager.ResetPasswordAsync(teacher.User, token, model.Password);
                if (!result.Succeeded)
                {
                    throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }

            teacher.EmployeeId = model.EmployeeId;
            teacher.JoiningDate = model.JoiningDate;
            teacher.Qualification = model.Qualification;
            teacher.Specialization = model.Specialization;
            teacher.ExperienceYears = model.ExperienceYears;
            teacher.Salary = model.Salary;
            teacher.Designation = model.Designation;
            teacher.EmergencyContact = model.EmergencyContact;
            teacher.IsActive = model.IsActive;
            teacher.UpdatedAt = DateTime.UtcNow;

            var existingAssignments = await _context.TeacherAssignments
                .Where(ta => ta.TeacherId == teacher.Id && ta.IsActive)
                .ToListAsync();

            foreach (var assignment in existingAssignments)
            {
                assignment.IsActive = false;
                assignment.UpdatedAt = DateTime.UtcNow;
            }

            if (model.AssignedClassIds.Any() && model.AssignedSubjectIds.Any())
            {
                var academicYear = "2024-2025";
                foreach (var classId in model.AssignedClassIds)
                {
                    foreach (var subjectId in model.AssignedSubjectIds)
                    {
                        var assignment = new TeacherAssignment
                        {
                            TeacherId = teacher.Id,
                            ClassId = classId,
                            SubjectId = subjectId,
                            AcademicYear = academicYear,
                            IsActive = true,
                            AssignedDate = DateTime.UtcNow,
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.TeacherAssignments.Add(assignment);
                    }
                }
            }

            await _context.SaveChangesAsync();

            await LogAuditAsync(updatedBy, "Update", "Teacher", teacher.Id.ToString(), oldValues,
                System.Text.Json.JsonSerializer.Serialize(teacher));

            return teacher;
        }

        public async Task<bool> DeleteTeacherAsync(int id, string deletedBy)
        {
            var teacher = await _context.Teachers
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (teacher == null) return false;

            var oldValues = System.Text.Json.JsonSerializer.Serialize(teacher);

            teacher.IsActive = false;
            teacher.User.IsActive = false;
            teacher.UpdatedAt = DateTime.UtcNow;

            var assignments = await _context.TeacherAssignments
                .Where(ta => ta.TeacherId == id && ta.IsActive)
                .ToListAsync();

            foreach (var assignment in assignments)
            {
                assignment.IsActive = false;
                assignment.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            await LogAuditAsync(deletedBy, "Delete", "Teacher", teacher.Id.ToString(), oldValues,
                System.Text.Json.JsonSerializer.Serialize(teacher));

            return true;
        }

        public async Task<bool> ActivateTeacherAsync(int id, string activatedBy)
        {
            var teacher = await _context.Teachers
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (teacher == null) return false;

            teacher.IsActive = true;
            teacher.User.IsActive = true;
            teacher.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeactivateTeacherAsync(int id, string deactivatedBy)
        {
            return await DeleteTeacherAsync(id, deactivatedBy);
        }

        public async Task<TeacherDashboardViewModel> GetTeacherDashboardAsync(string userId)
        {
            var teacher = await GetTeacherByUserIdAsync(userId);
            if (teacher == null) return new TeacherDashboardViewModel();

            var assignments = await _context.TeacherAssignments
                .Where(ta => ta.TeacherId == teacher.Id && ta.IsActive)
                .Include(ta => ta.Class)
                .Include(ta => ta.Subject)
                .ToListAsync();

            var assignedClasses = assignments.Select(a => a.Class).Distinct().ToList();
            var assignedSubjects = assignments.Select(a => a.Subject).Distinct().ToList();

            var classIds = assignedClasses.Select(c => c.Id).ToList();
            var assignedStudents = await _context.Students
                .Where(s => classIds.Contains(s.ClassId ?? 0) && s.IsActive)
                .Include(s => s.User)
                .ToListAsync();

            var notifications = await _context.Notifications
                .Where(n => n.TargetUserId == userId || (n.TargetRole == "Teacher" && n.IsGlobal))
                .OrderByDescending(n => n.CreatedAt)
                .Take(10)
                .ToListAsync();

            var pendingMarksCount = await _context.Marks
                .Where(m => m.TeacherId == teacher.Id && m.ObtainedMarks == 0m)
                .CountAsync();

            return new TeacherDashboardViewModel
            {
                Teacher = teacher,
                Assignments = assignments,
                AssignedClasses = assignedClasses,
                AssignedSubjects = assignedSubjects,
                AssignedStudents = assignedStudents,
                Notifications = notifications,
                TotalClasses = assignedClasses.Count,
                TotalStudents = assignedStudents.Count,
                TotalSubjects = assignedSubjects.Count,
                UnreadNotificationsCount = notifications.Count(n => !n.IsRead),
                PendingMarksCount = pendingMarksCount
            };
        }

        public async Task<List<SchoolClass>> GetAssignedClassesAsync(int teacherId)
        {
            return await _context.TeacherAssignments
                .Where(ta => ta.TeacherId == teacherId && ta.IsActive)
                .Select(ta => ta.Class)
                .Distinct()
                .ToListAsync();
        }

        public async Task<List<Subject>> GetAssignedSubjectsAsync(int teacherId)
        {
            return await _context.TeacherAssignments
                .Where(ta => ta.TeacherId == teacherId && ta.IsActive)
                .Select(ta => ta.Subject)
                .Distinct()
                .ToListAsync();
        }

        public async Task<List<Student>> GetAssignedStudentsAsync(int teacherId)
        {
            var classIds = await _context.TeacherAssignments
                .Where(ta => ta.TeacherId == teacherId && ta.IsActive)
                .Select(ta => ta.ClassId)
                .Distinct()
                .ToListAsync();

            return await _context.Students
                .Where(s => classIds.Contains(s.ClassId ?? 0) && s.IsActive)
                .Include(s => s.User)
                .ToListAsync();
        }

        public async Task<List<TeacherAssignment>> GetAssignmentsAsync(int teacherId)
        {
            return await _context.TeacherAssignments
                .Where(ta => ta.TeacherId == teacherId && ta.IsActive)
                .Include(ta => ta.Class)
                .Include(ta => ta.Subject)
                .ToListAsync();
        }

        public async Task<bool> IsTeacherAssignedToClassSubjectAsync(int teacherId, int classId, int subjectId)
        {
            return await _context.TeacherAssignments
                .AnyAsync(ta => ta.TeacherId == teacherId && ta.ClassId == classId && ta.SubjectId == subjectId && ta.IsActive);
        }
    }
}