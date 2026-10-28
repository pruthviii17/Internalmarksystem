using System.ComponentModel.DataAnnotations;

namespace InternalMarksSystem.Models
{
    public class ResultPublicationRecord
    {
        public int Id { get; set; }

        [Required]
        public int SubjectId { get; set; }
        public virtual Subject? Subject { get; set; }

        [Required]
        public int SemesterId { get; set; }
        public virtual Semester? Semester { get; set; }

        [Required]
        [MaxLength(20)]
        public string AcademicYear { get; set; } = "2024-2025";

        [Required]
        [MaxLength(450)]
        public string PublishedByUserId { get; set; } = string.Empty;

        public DateTime PublishedAtUtc { get; set; } = DateTime.UtcNow;

        public int TotalStudentsCount { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }
    }
}
