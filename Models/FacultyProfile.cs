using System.ComponentModel.DataAnnotations;

namespace InternalMarksSystem.Models
{
    public class FacultyProfile
    {
        public int Id { get; set; }

        public string? ApplicationUserId { get; set; }
        public virtual ApplicationUser? ApplicationUser { get; set; }

        [Required]
        [MaxLength(30)]
        [Display(Name = "Employee Code")]
        public string EmployeeCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Department")]
        public int DepartmentId { get; set; }
        public virtual Department? Department { get; set; }

        [Required]
        [MaxLength(80)]
        public string Designation { get; set; } = "Assistant Professor";

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual ICollection<FacultySubjectAssignment> AssignedSubjects { get; set; } = new List<FacultySubjectAssignment>();
    }
}
