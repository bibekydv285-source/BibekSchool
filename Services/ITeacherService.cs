using BibekSchool.Models;
using BibekSchool.ViewModels;

namespace BibekSchool.Services
{
    public interface ITeacherService
    {
        Task<Teacher?> GetTeacherByUserIdAsync(string userId);
        Task<Teacher?> GetTeacherByIdAsync(int id);
        Task<TeacherViewModel?> GetTeacherViewModelAsync(int id);
        Task<List<TeacherViewModel>> GetAllTeachersAsync();
        Task<Teacher> CreateTeacherAsync(TeacherViewModel model, string createdBy);
        Task<Teacher> UpdateTeacherAsync(TeacherViewModel model, string updatedBy);
        Task<bool> DeleteTeacherAsync(int id, string deletedBy);
        Task<bool> ActivateTeacherAsync(int id, string activatedBy);
        Task<bool> DeactivateTeacherAsync(int id, string deactivatedBy);
        Task<TeacherDashboardViewModel> GetTeacherDashboardAsync(string userId);
        Task<List<SchoolClass>> GetAssignedClassesAsync(int teacherId);
        Task<List<Subject>> GetAssignedSubjectsAsync(int teacherId);
        Task<List<Student>> GetAssignedStudentsAsync(int teacherId);
        Task<List<TeacherAssignment>> GetAssignmentsAsync(int teacherId);
        Task<bool> IsTeacherAssignedToClassSubjectAsync(int teacherId, int classId, int subjectId);
    }
}