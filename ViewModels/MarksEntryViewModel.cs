using InternalMarksSystem.Models;

namespace InternalMarksSystem.ViewModels
{
    public class StudentMarkRowViewModel
    {
        public int StudentId { get; set; }
        public string EnrollmentNumber { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public int? InternalMarkId { get; set; }

        // Component Id -> Marks Obtained
        public Dictionary<int, decimal> ComponentMarks { get; set; } = new();

        public decimal CalculatedTotal { get; set; }
        public decimal Percentage { get; set; }
        public string Grade { get; set; } = "-";
        public bool IsPass { get; set; }
        public MarkStatus Status { get; set; } = MarkStatus.Draft;
        public string? RejectionReason { get; set; }
    }

    public class MarksEntryViewModel
    {
        public int SubjectId { get; set; }
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public int SemesterId { get; set; }
        public string SemesterName { get; set; } = string.Empty;
        public string AcademicYear { get; set; } = "2024-2025";
        public decimal MaxInternalMarks { get; set; }
        public decimal PassingMarks { get; set; }

        public bool IsReadOnly { get; set; }
        public string StatusSummary { get; set; } = string.Empty;

        public List<AssessmentComponent> Components { get; set; } = new();
        public List<StudentMarkRowViewModel> Students { get; set; } = new();
    }
}
