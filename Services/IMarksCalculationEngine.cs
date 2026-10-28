using InternalMarksSystem.Models;

namespace InternalMarksSystem.Services
{
    public class ComponentScoreBreakdown
    {
        public int ComponentId { get; set; }
        public string ComponentName { get; set; } = string.Empty;
        public decimal MarksObtained { get; set; }
        public decimal MaxMarks { get; set; }
        public decimal WeightPercentage { get; set; }
        public decimal WeightedContribution { get; set; }
    }

    public class CalculationResult
    {
        public decimal CalculatedTotal { get; set; }
        public decimal MaxTotal { get; set; }
        public decimal Percentage { get; set; }
        public string Grade { get; set; } = "F";
        public bool IsPass { get; set; }
        public List<ComponentScoreBreakdown> Breakdowns { get; set; } = new();
    }

    public class SubjectSummaryStats
    {
        public int SubjectId { get; set; }
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public int TotalEvaluated { get; set; }
        public int PassedCount { get; set; }
        public int FailedCount { get; set; }
        public decimal PassPercentage { get; set; }
        public decimal AverageScore { get; set; }
        public decimal HighestScore { get; set; }
        public decimal LowestScore { get; set; }
        public Dictionary<string, int> GradeDistribution { get; set; } = new();
    }

    public class SemesterSummaryStats
    {
        public int SemesterId { get; set; }
        public string SemesterName { get; set; } = string.Empty;
        public int TotalStudents { get; set; }
        public int TotalRecords { get; set; }
        public int PassedCount { get; set; }
        public int FailedCount { get; set; }
        public decimal OverallPassPercentage { get; set; }
        public decimal OverallAverage { get; set; }
    }

    public interface IMarksCalculationEngine
    {
        CalculationResult Calculate(decimal maxInternalMarks, decimal passingMarks, IEnumerable<AssessmentComponent> components, IDictionary<int, decimal> componentMarks);
        bool ValidateComponentWeights(IEnumerable<AssessmentComponent> components, out string errorMessage);
        string DetermineGrade(decimal percentage);
        bool DeterminePassStatus(decimal totalScore, decimal passingMarks);
        SubjectSummaryStats CalculateSubjectStatistics(Subject subject, IEnumerable<InternalMark> marks);
        SemesterSummaryStats CalculateSemesterStatistics(Semester semester, IEnumerable<InternalMark> marks);
    }
}
