using BibekSchool.Models;
using BibekSchool.ViewModels;

namespace BibekSchool.Services
{
    public interface IResultService
    {
        Task<Result?> GetResultByIdAsync(int id);
        Task<ResultViewModel?> GetResultViewModelAsync(int id);
        Task<List<ResultViewModel>> GetResultsByStudentAsync(int studentId);
        Task<List<ResultViewModel>> GetResultsByClassAsync(int classId, string academicYear, string term);
        Task<Result> GenerateResultAsync(int studentId, string academicYear, string term, string generatedBy);
        Task<Result> PublishResultAsync(int resultId, string publishedBy);
        Task<bool> DeleteResultAsync(int id, string deletedBy);
        Task<string> CalculateOverallGradeAsync(decimal percentage);
        Task<int> CalculateRankAsync(int studentId, string academicYear, string term);
    }
}