using BibekSchool.Data;
using BibekSchool.Models;
using BibekSchool.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BibekSchool.Services
{
    public class ResultService : IResultService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMarkService _markService;

        public ResultService(ApplicationDbContext context, IMarkService markService)
        {
            _context = context;
            _markService = markService;
        }

        public async Task<Result?> GetResultByIdAsync(int id)
        {
            return await _context.Results
                .Include(r => r.Student)
                .ThenInclude(s => s.User)
                .Include(r => r.Student)
                .ThenInclude(s => s.Class)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<ResultViewModel?> GetResultViewModelAsync(int id)
        {
            var result = await GetResultByIdAsync(id);
            if (result == null) return null;

            var marks = await _context.Marks
                .Where(m => m.StudentId == result.StudentId && m.AcademicYear == result.AcademicYear)
                .Include(m => m.Subject)
                .ToListAsync();

            var subjectMarks = marks.Select(m => new MarkViewModel
            {
                Id = m.Id,
                StudentId = m.StudentId,
                SubjectId = m.SubjectId,
                SubjectName = m.Subject.Name,
                ExamName = m.ExamName,
                AcademicYear = m.AcademicYear,
                FullMarks = m.FullMarks,
                PassMarks = m.PassMarks,
                ObtainedMarks = m.ObtainedMarks,
                Grade = m.Grade,
                Remarks = m.Remarks,
                ExamDate = m.ExamDate
            }).ToList();

            return new ResultViewModel
            {
                Id = result.Id,
                StudentId = result.StudentId,
                AcademicYear = result.AcademicYear,
                Term = result.Term,
                TotalObtainedMarks = result.TotalObtainedMarks,
                TotalFullMarks = result.TotalFullMarks,
                Percentage = result.Percentage,
                OverallGrade = result.OverallGrade,
                RankInClass = result.RankInClass,
                TotalStudentsInClass = result.TotalStudentsInClass,
                IsPassed = result.IsPassed,
                Remarks = result.Remarks,
                PublishedDate = result.PublishedDate,
                PublishedBy = result.PublishedBy,
                StudentName = result.Student?.User.FullName,
                ClassName = result.Student?.Class?.Name,
                SubjectMarks = subjectMarks
            };
        }

        public async Task<List<ResultViewModel>> GetResultsByStudentAsync(int studentId)
        {
            return await _context.Results
                .Where(r => r.StudentId == studentId)
                .Include(r => r.Student)
                .ThenInclude(s => s.User)
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
                    StudentName = r.Student.User.FullName
                })
                .ToListAsync();
        }

        public async Task<List<ResultViewModel>> GetResultsByClassAsync(int classId, string academicYear, string term)
        {
            return await _context.Results
                .Where(r => r.Student.ClassId == classId && r.AcademicYear == academicYear && r.Term == term)
                .Include(r => r.Student)
                .ThenInclude(s => s.User)
                .OrderBy(r => r.RankInClass)
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
                    StudentName = r.Student.User.FullName
                })
                .ToListAsync();
        }

        public async Task<Result> GenerateResultAsync(int studentId, string academicYear, string term, string generatedBy)
        {
            var student = await _context.Students
                .Include(s => s.Class)
                .FirstOrDefaultAsync(s => s.Id == studentId);

            if (student == null)
                throw new Exception("Student not found");

            var marks = await _context.Marks
                .Where(m => m.StudentId == studentId && m.AcademicYear == academicYear)
                .Include(m => m.Subject)
                .ToListAsync();

            if (!marks.Any())
                throw new Exception("No marks found for this student in the specified academic year");

            var totalObtained = marks.Sum(m => m.ObtainedMarks);
            var totalFull = marks.Sum(m => m.FullMarks);
            var percentage = totalFull > 0 ? Math.Round((totalObtained / totalFull) * 100, 2) : 0;
            var overallGrade = await CalculateOverallGradeAsync(percentage);
            var isPassed = marks.All(m => m.IsPassed);
            var rank = await CalculateRankAsync(studentId, academicYear, term);
            var totalStudentsInClass = await _context.Students.CountAsync(s => s.ClassId == student.ClassId && s.IsActive);

            var existingResult = await _context.Results
                .FirstOrDefaultAsync(r => r.StudentId == studentId && r.AcademicYear == academicYear && r.Term == term);

            Result result;
            if (existingResult != null)
            {
                existingResult.TotalObtainedMarks = totalObtained;
                existingResult.TotalFullMarks = totalFull;
                existingResult.Percentage = percentage;
                existingResult.OverallGrade = overallGrade;
                existingResult.RankInClass = rank;
                existingResult.TotalStudentsInClass = totalStudentsInClass;
                existingResult.IsPassed = isPassed;
                existingResult.UpdatedAt = DateTime.UtcNow;
                result = existingResult;
            }
            else
            {
                result = new Result
                {
                    StudentId = studentId,
                    AcademicYear = academicYear,
                    Term = term,
                    TotalObtainedMarks = totalObtained,
                    TotalFullMarks = totalFull,
                    Percentage = percentage,
                    OverallGrade = overallGrade,
                    RankInClass = rank,
                    TotalStudentsInClass = totalStudentsInClass,
                    IsPassed = isPassed,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Results.Add(result);
            }

            await _context.SaveChangesAsync();

            await LogAuditAsync(generatedBy, "Generate", "Result", result.Id.ToString(), null,
                System.Text.Json.JsonSerializer.Serialize(result));

            return result;
        }

        public async Task<Result> PublishResultAsync(int resultId, string publishedBy)
        {
            var result = await _context.Results.FindAsync(resultId);
            if (result == null)
                throw new Exception("Result not found");

            var oldValues = System.Text.Json.JsonSerializer.Serialize(result);

            result.PublishedDate = DateTime.UtcNow;
            result.PublishedBy = publishedBy;
            result.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await LogAuditAsync(publishedBy, "Publish", "Result", result.Id.ToString(), oldValues,
                System.Text.Json.JsonSerializer.Serialize(result));

            await CreateNotificationAsync(
                $"Your {result.Term} result for {result.AcademicYear} has been published.",
                "Result Published",
                result.Student.UserId,
                "Student",
                $"/Student/Results");

            return result;
        }

        public async Task<bool> DeleteResultAsync(int id, string deletedBy)
        {
            var result = await _context.Results.FindAsync(id);
            if (result == null) return false;

            var oldValues = System.Text.Json.JsonSerializer.Serialize(result);

            _context.Results.Remove(result);
            await _context.SaveChangesAsync();

            await LogAuditAsync(deletedBy, "Delete", "Result", id.ToString(), oldValues, null);
            return true;
        }

        public async Task<string> CalculateOverallGradeAsync(decimal percentage)
        {
            return (double)percentage switch
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

        public async Task<int> CalculateRankAsync(int studentId, string academicYear, string term)
        {
            var student = await _context.Students
                .Include(s => s.Class)
                .FirstOrDefaultAsync(s => s.Id == studentId);

            if (student?.ClassId == null) return 0;

            var classResults = await _context.Results
                .Where(r => r.Student.ClassId == student.ClassId && r.AcademicYear == academicYear && r.Term == term)
                .OrderByDescending(r => r.Percentage)
                .ToListAsync();

            var rank = classResults.FindIndex(r => r.StudentId == studentId) + 1;
            return rank > 0 ? rank : 0;
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

        private async Task CreateNotificationAsync(string message, string title, string targetUserId, string targetRole, string referenceLink)
        {
            var notification = new Notification
            {
                Title = title,
                Message = message,
                Type = "Info",
                TargetUserId = targetUserId,
                TargetRole = targetRole,
                IsGlobal = false,
                ReferenceLink = referenceLink,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System"
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }
    }
}