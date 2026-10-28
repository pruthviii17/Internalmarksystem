using InternalMarksSystem.Models;

namespace InternalMarksSystem.ViewModels
{
    public class StudentSubjectResultItem
    {
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public int Credits { get; set; }
        public decimal MaxMarks { get; set; }
        public decimal PassingMarks { get; set; }
        public decimal ObtainedMarks { get; set; }
        public decimal Percentage { get; set; }
        public string Grade { get; set; } = string.Empty;
        public bool IsPass { get; set; }
        public List<InternalMarkDetail> ComponentDetails { get; set; } = new();
    }

    public class StudentReportViewModel
    {
        public Student? Student { get; set; }
        public Semester? Semester { get; set; }
        public List<StudentSubjectResultItem> Results { get; set; } = new();
        public decimal TotalMaxMarks { get; set; }
        public decimal TotalObtainedMarks { get; set; }
        public decimal OverallPercentage { get; set; }
        public string OverallResult { get; set; } = "Pass";
        public int TotalCredits { get; set; }
    }
}
