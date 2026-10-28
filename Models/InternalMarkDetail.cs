using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternalMarksSystem.Models
{
    public class InternalMarkDetail
    {
        public int Id { get; set; }

        [Required]
        public int InternalMarkId { get; set; }
        public virtual InternalMark? InternalMark { get; set; }

        [Required]
        public int AssessmentComponentId { get; set; }
        public virtual AssessmentComponent? AssessmentComponent { get; set; }

        [Required]
        [Range(0, 100)]
        [Column(TypeName = "decimal(6,2)")]
        [Display(Name = "Marks Obtained")]
        public decimal MarksObtained { get; set; } = 0m;
    }
}
