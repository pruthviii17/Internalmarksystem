using System.ComponentModel.DataAnnotations;

namespace InternalMarksSystem.Models
{
    public class Student
    {
        public int Id { get; set; }

        public string? ApplicationUserId { get; set; }
        public virtual ApplicationUser? ApplicationUser { get; set; }

        [Required]
        [MaxLength(30)]
        [Display(Name = "Enrollment Number")]
        public string EnrollmentNumber { get; set; } = string.Empty;

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
        [Display(Name = "Semester")]
        public int SemesterId { get; set; }
        public virtual Semester? Semester { get; set; }

        [Required]
        [Range(2000, 2100)]
        [Display(Name = "Enrollment Year")]
        public int EnrollmentYear { get; set; } = 2022;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual ICollection<InternalMark> InternalMarks { get; set; } = new List<InternalMark>();
    }
}
