using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternalMarksSystem.Models
{
    public class AssessmentComponent
    {
        public int Id { get; set; }

        [Required]
        public int SubjectId { get; set; }
        public virtual Subject? Subject { get; set; }

        [Required]
        [MaxLength(100)]
        [Display(Name = "Component Name")]
        public string Name { get; set; } = string.Empty; // e.g. "Internal Test 1", "Internal Test 2", "Assignments", "Practical/Viva"

        [Required]
        [Range(1, 100)]
        [Column(TypeName = "decimal(6,2)")]
        [Display(Name = "Max Marks")]
        public decimal MaxMarks { get; set; } = 30m;

        [Required]
        [Range(1, 100)]
        [Column(TypeName = "decimal(6,2)")]
        [Display(Name = "Weight Percentage (%)")]
        public decimal WeightPercentage { get; set; } = 30m;

        public int DisplayOrder { get; set; } = 1;

        // Navigation
        public virtual ICollection<InternalMarkDetail> MarkDetails { get; set; } = new List<InternalMarkDetail>();
    }
}
