using BibekSchool.Data;
using BibekSchool.Models;
using BibekSchool.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BibekSchool.Services
{
    public class MarkService : IMarkService
    {
        private readonly ApplicationDbContext _context;

        public MarkService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Mark?> GetMarkByIdAsync(int id)
        {
            return await _context.Marks
                .Include(m => m.Student)
                .ThenInclude(s => s.User)
                .Include(m => m.Subject)
                .Include(m => m.Teacher)
                .ThenInclude(t => t.User)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<MarkViewModel?> GetMarkViewModelAsync(int id)
        {
            var mark = await GetMarkByIdAsync(id);
            if (mark == null) return null;

            return new MarkViewModel
            {
                Id = mark.Id,
                StudentId = mark.StudentId,
                SubjectId = mark.SubjectId,
                TeacherId = mark.TeacherId,
                ExamName = mark.ExamName,
                AcademicYear = mark.AcademicYear,
                FullMarks = mark.FullMarks,
                PassMarks = mark.PassMarks,
                ObtainedMarks = mark.ObtainedMarks,
                Grade = mark.Grade,
                Remarks = mark.Remarks,
                ExamDate = mark.ExamDate,
                StudentName = mark.Student?.User.FullName,
                SubjectName = mark.Subject?.Name,
                TeacherName = mark.Teacher?.User.FullName
            };
        }

        public async Task<List<MarkViewModel>> GetMarksByStudentAsync(int studentId, string? academicYear = null)
        {
            IQueryable<Mark> query = _context.Marks
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
                    SubjectName = m.Subject.Name,
                    TeacherName = m.Teacher.User.FullName
                })
                .ToListAsync();
        }

        public async Task<List<MarkViewModel>> GetMarksByTeacherAsync(int teacherId, string? academicYear = null)
        {
            IQueryable<Mark> query = _context.Marks
                .Where(m => m.TeacherId == teacherId)
                .Include(m => m.Student)
                .ThenInclude(s => s.User)
                .Include(m => m.Subject);

            if (!string.IsNullOrEmpty(academicYear))
            {
                query = query.Where(m => m.AcademicYear == academicYear);
            }

            return await query
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
                    SubjectName = m.Subject.Name
                })
                .ToListAsync();
        }

        public async Task<List<MarkViewModel>> GetMarksByClassSubjectAsync(int classId, int subjectId, string? academicYear = null, string? examName = null)
        {
            var studentIds = await _context.Students
                .Where(s => s.ClassId == classId && s.IsActive)
                .Select(s => s.Id)
                .ToListAsync();

            IQueryable<Mark> query = _context.Marks
                .Where(m => studentIds.Contains(m.StudentId) && m.SubjectId == subjectId);

            if (!string.IsNullOrEmpty(academicYear))
            {
                query = query.Where(m => m.AcademicYear == academicYear);
            }

            if (!string.IsNullOrEmpty(examName))
            {
                query = query.Where(m => m.ExamName == examName);
            }

            return await query
                .Include(m => m.Student)
                .ThenInclude(s => s.User)
                .Include(m => m.Teacher)
                .ThenInclude(t => t.User)
                .OrderBy(m => m.Student.User.FullName)
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
                    TeacherName = m.Teacher.User.FullName
                })
                .ToListAsync();
        }

        public async Task<Mark> CreateMarkAsync(MarkViewModel model, string createdBy)
        {
            var grade = await CalculateGradeAsync(model.ObtainedMarks, model.FullMarks, model.PassMarks);

            var mark = new Mark
            {
                StudentId = model.StudentId,
                SubjectId = model.SubjectId,
                TeacherId = model.TeacherId,
                ExamName = model.ExamName,
                AcademicYear = model.AcademicYear,
                FullMarks = model.FullMarks,
                PassMarks = model.PassMarks,
                ObtainedMarks = model.ObtainedMarks,
                Grade = grade,
                Remarks = model.Remarks,
                ExamDate = model.ExamDate,
                CreatedAt = DateTime.UtcNow
            };

            _context.Marks.Add(mark);
            await _context.SaveChangesAsync();

            await LogAuditAsync(createdBy, "Create", "Mark", mark.Id.ToString(), null,
                System.Text.Json.JsonSerializer.Serialize(mark));

            return mark;
        }

        public async Task<Mark> UpdateMarkAsync(MarkViewModel model, string updatedBy)
        {
            var mark = await _context.Marks.FindAsync(model.Id);
            if (mark == null)
                throw new Exception("Mark not found");

            var oldValues = System.Text.Json.JsonSerializer.Serialize(mark);

            var grade = await CalculateGradeAsync(model.ObtainedMarks, model.FullMarks, model.PassMarks);

            mark.StudentId = model.StudentId;
            mark.SubjectId = model.SubjectId;
            mark.TeacherId = model.TeacherId;
            mark.ExamName = model.ExamName;
            mark.AcademicYear = model.AcademicYear;
            mark.FullMarks = model.FullMarks;
            mark.PassMarks = model.PassMarks;
            mark.ObtainedMarks = model.ObtainedMarks;
            mark.Grade = grade;
            mark.Remarks = model.Remarks;
            mark.ExamDate = model.ExamDate;
            mark.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await LogAuditAsync(updatedBy, "Update", "Mark", mark.Id.ToString(), oldValues,
                System.Text.Json.JsonSerializer.Serialize(mark));

            return mark;
        }

        public async Task<bool> DeleteMarkAsync(int id, string deletedBy)
        {
            var mark = await _context.Marks.FindAsync(id);
            if (mark == null) return false;

            var oldValues = System.Text.Json.JsonSerializer.Serialize(mark);

            _context.Marks.Remove(mark);
            await _context.SaveChangesAsync();

            await LogAuditAsync(deletedBy, "Delete", "Mark", id.ToString(), oldValues, null);
            return true;
        }

        public async Task<bool> BulkCreateMarksAsync(List<MarkViewModel> models, string createdBy)
        {
            var marks = new List<Mark>();

            foreach (var model in models)
            {
                var grade = await CalculateGradeAsync(model.ObtainedMarks, model.FullMarks, model.PassMarks);

                marks.Add(new Mark
                {
                    StudentId = model.StudentId,
                    SubjectId = model.SubjectId,
                    TeacherId = model.TeacherId,
                    ExamName = model.ExamName,
                    AcademicYear = model.AcademicYear,
                    FullMarks = model.FullMarks,
                    PassMarks = model.PassMarks,
                    ObtainedMarks = model.ObtainedMarks,
                    Grade = grade,
                    Remarks = model.Remarks,
                    ExamDate = model.ExamDate,
                    CreatedAt = DateTime.UtcNow
                });
            }

            _context.Marks.AddRange(marks);
            await _context.SaveChangesAsync();

            foreach (var mark in marks)
            {
                await LogAuditAsync(createdBy, "Create", "Mark", mark.Id.ToString(), null,
                    System.Text.Json.JsonSerializer.Serialize(mark));
            }

            return true;
        }

        public async Task<string> CalculateGradeAsync(decimal obtainedMarks, int fullMarks, int passMarks)
        {
            if (obtainedMarks < passMarks)
                return "F";

            var percentage = (double)obtainedMarks / fullMarks * 100;

            return percentage switch
            {
                >= 90 => "A+",
                >= 80 => "A",
                >= 70 => "B+",
                >= 60 => "B",
                >= 50 => "C+",
                >= 40 => "C",
                _ => "D"
            };
        }

        public async Task<decimal> CalculatePercentageAsync(decimal obtainedMarks, int fullMarks)
        {
            if (fullMarks == 0) return 0;
            return Math.Round((obtainedMarks / fullMarks) * 100, 2);
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