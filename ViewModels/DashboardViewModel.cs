using InternalMarksSystem.Models;

namespace InternalMarksSystem.ViewModels
{
    public class DashboardViewModel
    {
        public string UserRole { get; set; } = "Student";
        public string UserName { get; set; } = string.Empty;
        public string UserFullName { get; set; } = string.Empty;

        // Metric Cards
        public int TotalStudents { get; set; }
        public int TotalSubjects { get; set; }
        public int TotalFaculty { get; set; }
        public int TotalMarksRecords { get; set; }
        public int PendingApprovals { get; set; }
        public int PublishedResults { get; set; }

        // Faculty specific
        public List<Subject> AssignedSubjects { get; set; } = new();
        public int MyDraftMarksCount { get; set; }
        public int MySubmittedMarksCount { get; set; }

        // Student specific
        public Student? StudentProfile { get; set; }
        public List<InternalMark> StudentPublishedMarks { get; set; } = new();
        public decimal StudentOverallPercentage { get; set; }
        public int StudentPassedSubjectsCount { get; set; }
        public int StudentTotalSubjectsCount { get; set; }

        // Chart Data
        public List<string> SubjectNames { get; set; } = new();
        public List<decimal> SubjectAverages { get; set; } = new();
        public int PassCount { get; set; }
        public int FailCount { get; set; }
        public Dictionary<string, int> GradeDistribution { get; set; } = new();

        // Recent Activity
        public List<AuditLog> RecentActivities { get; set; } = new();
    }
}
