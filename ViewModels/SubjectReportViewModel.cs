using InternalMarksSystem.Models;
using InternalMarksSystem.Services;

namespace InternalMarksSystem.ViewModels
{
    public class SubjectReportViewModel
    {
        public Subject? Subject { get; set; }
        public Semester? Semester { get; set; }
        public SubjectSummaryStats Statistics { get; set; } = new();
        public List<InternalMark> MarksList { get; set; } = new();
    }
}
