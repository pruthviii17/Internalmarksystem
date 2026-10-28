using System.ComponentModel.DataAnnotations;

namespace InternalMarksSystem.ViewModels
{
    public class ApprovalActionViewModel
    {
        [Required]
        public int SubjectId { get; set; }

        [Required]
        public int SemesterId { get; set; }

        [Required]
        public string AcademicYear { get; set; } = "2024-2025";

        [Required]
        public string ActionType { get; set; } = "Approve"; // Approve, Reject, Publish

        [MaxLength(500)]
        [Display(Name = "Rejection Reason (Required when rejecting)")]
        public string? Reason { get; set; }
    }

    public class ApprovalSubmissionGroupViewModel
    {
        public int SubjectId { get; set; }
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public int SemesterId { get; set; }
        public string SemesterName { get; set; } = string.Empty;
        public string AcademicYear { get; set; } = string.Empty;
        public string? FacultyName { get; set; }
        public int TotalRecords { get; set; }
        public int DraftCount { get; set; }
        public int SubmittedCount { get; set; }
        public int ApprovedCount { get; set; }
        public int PublishedCount { get; set; }
        public int RejectedCount { get; set; }
        public DateTime? LatestSubmittedAt { get; set; }
    }
}
