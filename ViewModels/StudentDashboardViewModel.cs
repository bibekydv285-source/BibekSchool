using BibekSchool.Models;

namespace BibekSchool.ViewModels
{
    public class StudentDashboardViewModel
    {
        public Student? Student { get; set; }
        public SchoolClass? CurrentClass { get; set; }
        public List<Subject> Subjects { get; set; } = new();
        public List<Mark> RecentMarks { get; set; } = new();
        public List<Notification> Notifications { get; set; } = new();
        public Result? LatestResult { get; set; }
        public int TotalSubjects { get; set; }
        public decimal AveragePercentage { get; set; }
        public int UnreadNotificationsCount { get; set; }
    }
}