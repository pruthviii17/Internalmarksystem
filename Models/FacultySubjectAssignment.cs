using System.ComponentModel.DataAnnotations;

namespace InternalMarksSystem.Models
{
    public class FacultySubjectAssignment
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Faculty")]
        public int FacultyProfileId { get; set; }
        public virtual FacultyProfile? FacultyProfile { get; set; }

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

        public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
