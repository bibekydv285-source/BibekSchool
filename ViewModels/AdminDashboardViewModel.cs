using BibekSchool.Models;

namespace BibekSchool.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalStudents { get; set; }
        public int TotalTeachers { get; set; }
        public int TotalClasses { get; set; }
        public int TotalSubjects { get; set; }
        public int ActiveStudents { get; set; }
        public int ActiveTeachers { get; set; }
        public List<ApplicationUser> RecentRegistrations { get; set; } = new();
        public List<Result> RecentResults { get; set; } = new();
        public List<AuditLog> RecentActivities { get; set; } = new();
        public List<Notification> Notifications { get; set; } = new();
        public int UnreadNotificationsCount { get; set; }
    }
}