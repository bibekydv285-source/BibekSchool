using BibekSchool.Models;
using BibekSchool.ViewModels;

namespace BibekSchool.Services
{
    public interface IStudentService
    {
        Task<Student?> GetStudentByUserIdAsync(string userId);
        Task<Student?> GetStudentByIdAsync(int id);
        Task<StudentViewModel?> GetStudentViewModelAsync(int id);
        Task<List<StudentViewModel>> GetAllStudentsAsync();
        Task<List<StudentViewModel>> GetStudentsByClassAsync(int classId);
        Task<Student> CreateStudentAsync(StudentViewModel model, string createdBy);
        Task<Student> UpdateStudentAsync(StudentViewModel model, string updatedBy);
        Task<bool> DeleteStudentAsync(int id, string deletedBy);
        Task<bool> ActivateStudentAsync(int id, string activatedBy);
        Task<bool> DeactivateStudentAsync(int id, string deactivatedBy);
        Task<StudentDashboardViewModel> GetStudentDashboardAsync(string userId);
        Task<List<Mark>> GetStudentMarksAsync(int studentId, string? academicYear = null);
        Task<Result?> GetStudentLatestResultAsync(int studentId);
        Task<bool> IsStudentInClassAsync(int studentId, int classId);
    }
}