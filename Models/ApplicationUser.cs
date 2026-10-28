using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace InternalMarksSystem.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [MaxLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [MaxLength(30)]
        public string UserRole { get; set; } = "Student"; // Admin, Faculty, Student

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Student? StudentProfile { get; set; }
        public virtual FacultyProfile? FacultyProfile { get; set; }
    }
}
