using System.ComponentModel.DataAnnotations;

namespace InternalMarksSystem.Models
{
    public class Semester
    {
        public int Id { get; set; }

        [Required]
        [Range(1, 10)]
        [Display(Name = "Semester Number")]
        public int SemesterNumber { get; set; }

        [Required]
        [MaxLength(50)]
        [Display(Name = "Semester Name")]
        public string Name { get; set; } = string.Empty; // e.g., "Semester 5"

        [Required]
        [MaxLength(20)]
        [Display(Name = "Academic Year")]
        public string AcademicYear { get; set; } = "2024-2025";

        public bool IsActive { get; set; } = true;

        // Navigation
        public virtual ICollection<Subject> Subjects { get; set; } = new List<Subject>();
        public virtual ICollection<Student> Students { get; set; } = new List<Student>();
    }
}
