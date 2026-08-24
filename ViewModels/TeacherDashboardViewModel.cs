using BibekSchool.Models;

namespace BibekSchool.ViewModels
{
    public class TeacherDashboardViewModel
    {
        public Teacher? Teacher { get; set; }
        public List<TeacherAssignment> Assignments { get; set; } = new();
        public List<SchoolClass> AssignedClasses { get; set; } = new();
        public List<Subject> AssignedSubjects { get; set; } = new();
        public List<Student> AssignedStudents { get; set; } = new();
        public List<Notification> Notifications { get; set; } = new();
        public int TotalClasses { get; set; }
        public int TotalStudents { get; set; }
        public int TotalSubjects { get; set; }
        public int UnreadNotificationsCount { get; set; }
        public int PendingMarksCount { get; set; }
    }
}