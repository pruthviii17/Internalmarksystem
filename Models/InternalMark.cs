using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternalMarksSystem.Models
{
    public class InternalMark
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Student")]
        public int StudentId { get; set; }
        public virtual Student? Student { get; set; }

        [Required]
        [Display(Name = "Subject")]
        public int SubjectId { get; set; }
        public virtual Subject? Subject { get; set; }

        [Required]
        [Display(Name = "Semester")]
        public int SemesterId { get; set; }
        public virtual Semester? Semester { get; set; }

        [Required]
        [MaxLength(20)]
        [Display(Name = "Academic Year")]
        public string AcademicYear { get; set; } = "2024-2025";

        [Column(TypeName = "decimal(6,2)")]
        [Display(Name = "Calculated Total")]
        public decimal CalculatedTotal { get; set; } = 0m;

        [Column(TypeName = "decimal(6,2)")]
        [Display(Name = "Max Total")]
        public decimal MaxTotal { get; set; } = 50m;

        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "Percentage")]
        public decimal Percentage { get; set; } = 0m;

        [MaxLength(10)]
        public string Grade { get; set; } = "F";

        [Display(Name = "Pass Status")]
        public bool IsPass { get; set; } = false;

        public MarkStatus Status { get; set; } = MarkStatus.Draft;

        [MaxLength(450)]
        public string? SubmittedByUserId { get; set; }
        public DateTime? SubmittedAtUtc { get; set; }

        [MaxLength(450)]
        public string? ApprovedByUserId { get; set; }
        public DateTime? ApprovedAtUtc { get; set; }

        [MaxLength(450)]
        public string? PublishedByUserId { get; set; }
        public DateTime? PublishedAtUtc { get; set; }

        [MaxLength(500)]
        [Display(Name = "Rejection Reason")]
        public string? RejectionReason { get; set; }

        [MaxLength(500)]
        public string? Remarks { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual ICollection<InternalMarkDetail> Details { get; set; } = new List<InternalMarkDetail>();
    }
}
