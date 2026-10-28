using InternalMarksSystem.Models;

namespace InternalMarksSystem.Services
{
    public class MarksCalculationEngine : IMarksCalculationEngine
    {
        public CalculationResult Calculate(
            decimal maxInternalMarks,
            decimal passingMarks,
            IEnumerable<AssessmentComponent> components,
            IDictionary<int, decimal> componentMarks)
        {
            var componentList = components?.ToList() ?? new List<AssessmentComponent>();
            var result = new CalculationResult
            {
                MaxTotal = maxInternalMarks,
                Breakdowns = new List<ComponentScoreBreakdown>()
            };

            if (!componentList.Any() || maxInternalMarks <= 0)
            {
                return result;
            }

            decimal totalWeights = componentList.Sum(c => c.WeightPercentage);
            if (totalWeights <= 0) totalWeights = 100m;

            decimal totalCalculatedScore = 0m;

            foreach (var comp in componentList)
            {
                componentMarks.TryGetValue(comp.Id, out decimal marksObtained);

                // Ensure marks obtained is between 0 and component's MaxMarks
                decimal clampedMarks = Math.Max(0m, Math.Min(marksObtained, comp.MaxMarks));

                // Component allocation out of subject max internal marks
                decimal componentWeightRatio = comp.WeightPercentage / totalWeights;
                decimal componentMaxContribution = maxInternalMarks * componentWeightRatio;

                decimal weightedContribution = 0m;
                if (comp.MaxMarks > 0)
                {
                    weightedContribution = (clampedMarks / comp.MaxMarks) * componentMaxContribution;
                }

                weightedContribution = Math.Round(weightedContribution, 2, MidpointRounding.AwayFromZero);
                totalCalculatedScore += weightedContribution;

                result.Breakdowns.Add(new ComponentScoreBreakdown
                {
                    ComponentId = comp.Id,
                    ComponentName = comp.Name,
                    MarksObtained = clampedMarks,
                    MaxMarks = comp.MaxMarks,
                    WeightPercentage = comp.WeightPercentage,
                    WeightedContribution = weightedContribution
                });
            }

            // Cap calculated score at maxInternalMarks
            result.CalculatedTotal = Math.Min(totalCalculatedScore, maxInternalMarks);
            result.CalculatedTotal = Math.Round(result.CalculatedTotal, 2, MidpointRounding.AwayFromZero);

            // Calculate percentage
            if (maxInternalMarks > 0)
            {
                result.Percentage = Math.Round((result.CalculatedTotal / maxInternalMarks) * 100m, 2, MidpointRounding.AwayFromZero);
            }
            else
            {
                result.Percentage = 0m;
            }

            result.Grade = DetermineGrade(result.Percentage);
            result.IsPass = DeterminePassStatus(result.CalculatedTotal, passingMarks);

            return result;
        }

        public bool ValidateComponentWeights(IEnumerable<AssessmentComponent> components, out string errorMessage)
        {
            errorMessage = string.Empty;
            var list = components?.ToList() ?? new List<AssessmentComponent>();
            if (!list.Any())
            {
                errorMessage = "At least one assessment component is required.";
                return false;
            }

            foreach (var item in list)
            {
                if (item.MaxMarks <= 0)
                {
                    errorMessage = $"Component '{item.Name}' must have maximum marks greater than zero.";
                    return false;
                }
                if (item.WeightPercentage <= 0)
                {
                    errorMessage = $"Component '{item.Name}' must have a weight percentage greater than zero.";
                    return false;
                }
            }

            decimal totalWeight = list.Sum(c => c.WeightPercentage);
            if (Math.Abs(totalWeight - 100m) > 0.01m)
            {
                errorMessage = $"Sum of component weights must equal 100%. Current total is {totalWeight:0.##}%.";
                return false;
            }

            return true;
        }

        public string DetermineGrade(decimal percentage)
        {
            return percentage switch
            {
                >= 90m => "O",
                >= 80m => "A+",
                >= 70m => "A",
                >= 60m => "B+",
                >= 50m => "B",
                >= 40m => "C",
                _ => "F"
            };
        }

        public bool DeterminePassStatus(decimal totalScore, decimal passingMarks)
        {
            return totalScore >= passingMarks;
        }

        public SubjectSummaryStats CalculateSubjectStatistics(Subject subject, IEnumerable<InternalMark> marks)
        {
            var markList = marks?.ToList() ?? new List<InternalMark>();

            var stats = new SubjectSummaryStats
            {
                SubjectId = subject.Id,
                SubjectCode = subject.SubjectCode,
                SubjectName = subject.SubjectName,
                TotalEvaluated = markList.Count,
                GradeDistribution = new Dictionary<string, int>
                {
                    { "O", 0 }, { "A+", 0 }, { "A", 0 }, { "B+", 0 }, { "B", 0 }, { "C", 0 }, { "F", 0 }
                }
            };

            if (!markList.Any())
            {
                return stats;
            }

            stats.PassedCount = markList.Count(m => m.IsPass);
            stats.FailedCount = markList.Count(m => !m.IsPass);
            stats.PassPercentage = stats.TotalEvaluated > 0
                ? Math.Round(((decimal)stats.PassedCount / stats.TotalEvaluated) * 100m, 2)
                : 0m;

            stats.AverageScore = Math.Round(markList.Average(m => m.CalculatedTotal), 2);
            stats.HighestScore = markList.Max(m => m.CalculatedTotal);
            stats.LowestScore = markList.Min(m => m.CalculatedTotal);

            foreach (var m in markList)
            {
                string grade = string.IsNullOrWhiteSpace(m.Grade) ? "F" : m.Grade;
                if (stats.GradeDistribution.ContainsKey(grade))
                {
                    stats.GradeDistribution[grade]++;
                }
                else
                {
                    stats.GradeDistribution[grade] = 1;
                }
            }

            return stats;
        }

        public SemesterSummaryStats CalculateSemesterStatistics(Semester semester, IEnumerable<InternalMark> marks)
        {
            var markList = marks?.ToList() ?? new List<InternalMark>();
            var stats = new SemesterSummaryStats
            {
                SemesterId = semester.Id,
                SemesterName = semester.Name,
                TotalRecords = markList.Count,
                TotalStudents = markList.Select(m => m.StudentId).Distinct().Count()
            };

            if (!markList.Any()) return stats;

            stats.PassedCount = markList.Count(m => m.IsPass);
            stats.FailedCount = markList.Count(m => !m.IsPass);
            stats.OverallPassPercentage = stats.TotalRecords > 0
                ? Math.Round(((decimal)stats.PassedCount / stats.TotalRecords) * 100m, 2)
                : 0m;
            stats.OverallAverage = Math.Round(markList.Average(m => m.CalculatedTotal), 2);

            return stats;
        }
    }
}
