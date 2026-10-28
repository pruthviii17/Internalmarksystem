using System.ComponentModel.DataAnnotations;

namespace InternalMarksSystem.Models
{
    public class Department
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        [Display(Name = "Department Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(10)]
        [Display(Name = "Department Code")]
        public string Code { get; set; } = string.Empty;

        [MaxLength(250)]
        public string? Description { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual ICollection<Subject> Subjects { get; set; } = new List<Subject>();
        public virtual ICollection<Student> Students { get; set; } = new List<Student>();
        public virtual ICollection<FacultyProfile> FacultyMembers { get; set; } = new List<FacultyProfile>();
    }
}
