using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternalMarksSystem.Models
{
    public class Subject
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        [Display(Name = "Subject Code")]
        public string SubjectCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        [Display(Name = "Subject Name")]
        public string SubjectName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Department")]
        public int DepartmentId { get; set; }
        public virtual Department? Department { get; set; }

        [Required]
        [Display(Name = "Semester")]
        public int SemesterId { get; set; }
        public virtual Semester? Semester { get; set; }

        [Required]
        [Range(1, 10)]
        public int Credits { get; set; } = 4;

        [Required]
        [Range(10, 200)]
        [Column(TypeName = "decimal(6,2)")]
        [Display(Name = "Max Internal Marks")]
        public decimal MaxInternalMarks { get; set; } = 50m;

        [Required]
        [Range(1, 200)]
        [Column(TypeName = "decimal(6,2)")]
        [Display(Name = "Passing Marks")]
        public decimal PassingMarks { get; set; } = 20m;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual ICollection<AssessmentComponent> AssessmentComponents { get; set; } = new List<AssessmentComponent>();
        public virtual ICollection<FacultySubjectAssignment> FacultyAssignments { get; set; } = new List<FacultySubjectAssignment>();
        public virtual ICollection<InternalMark> InternalMarks { get; set; } = new List<InternalMark>();
    }
}
