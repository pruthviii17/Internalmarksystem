using System.ComponentModel.DataAnnotations;

namespace InternalMarksSystem.Models
{
    public class AuditLog
    {
        public int Id { get; set; }

        [MaxLength(450)]
        public string? UserId { get; set; }

        [MaxLength(100)]
        public string? UserName { get; set; }

        [MaxLength(50)]
        public string? UserRole { get; set; }

        [Required]
        [MaxLength(100)]
        public string Action { get; set; } = string.Empty; // e.g., "Create Marks", "Update Marks", "Submit Marks", "Approve", "Reject", "Publish"

        [MaxLength(100)]
        public string? EntityName { get; set; }

        [MaxLength(100)]
        public string? EntityId { get; set; }

        [MaxLength(2000)]
        public string? Details { get; set; }

        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

        [MaxLength(50)]
        public string? IpAddress { get; set; }
    }
}
