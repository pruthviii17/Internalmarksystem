using InternalMarksSystem.Models;
using System.ComponentModel.DataAnnotations;

namespace InternalMarksSystem.ViewModels
{
    public class AssessmentComponentInputModel
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Range(1, 100)]
        public decimal MaxMarks { get; set; } = 30m;

        [Required]
        [Range(1, 100)]
        public decimal WeightPercentage { get; set; } = 30m;

        public int DisplayOrder { get; set; } = 1;
    }

    public class SubjectComponentConfigViewModel
    {
        public int SubjectId { get; set; }
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public decimal MaxInternalMarks { get; set; }
        public decimal PassingMarks { get; set; }

        public List<AssessmentComponentInputModel> Components { get; set; } = new();
    }
}
