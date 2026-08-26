using BibekSchool.Models;
using BibekSchool.ViewModels;

namespace BibekSchool.Services
{
    public interface IMarkService
    {
        Task<Mark?> GetMarkByIdAsync(int id);
        Task<MarkViewModel?> GetMarkViewModelAsync(int id);
        Task<List<MarkViewModel>> GetMarksByStudentAsync(int studentId, string? academicYear = null);
        Task<List<MarkViewModel>> GetMarksByTeacherAsync(int teacherId, string? academicYear = null);
        Task<List<MarkViewModel>> GetMarksByClassSubjectAsync(int classId, int subjectId, string? academicYear = null, string? examName = null);
        Task<Mark> CreateMarkAsync(MarkViewModel model, string createdBy);
        Task<Mark> UpdateMarkAsync(MarkViewModel model, string updatedBy);
        Task<bool> DeleteMarkAsync(int id, string deletedBy);
        Task<bool> BulkCreateMarksAsync(List<MarkViewModel> models, string createdBy);
        string CalculateGrade(decimal obtainedMarks, int fullMarks, int passMarks);
        Task<decimal> CalculatePercentageAsync(decimal obtainedMarks, int fullMarks);
    }
}