using System;
using System.Collections.Generic;
using InternalMarksSystem.Models;
using InternalMarksSystem.Services;

namespace InternalMarksSystem
{
    public static class VerificationRunner
    {
        public static void RunTests()
        {
            Console.WriteLine("=================================================");
            Console.WriteLine("INTERNAL MARKS CALCULATION ENGINE VERIFICATION");
            Console.WriteLine("=================================================");

            var engine = new MarksCalculationEngine();

            // 1. Define Assessment Scheme for Subject CS501 (Max 50, Pass 20)
            var components = new List<AssessmentComponent>
            {
                new() { Id = 1, Name = "Internal Test 1", MaxMarks = 30m, WeightPercentage = 30m },
                new() { Id = 2, Name = "Internal Test 2", MaxMarks = 30m, WeightPercentage = 30m },
                new() { Id = 3, Name = "Assignments", MaxMarks = 20m, WeightPercentage = 20m },
                new() { Id = 4, Name = "Practical / Viva", MaxMarks = 20m, WeightPercentage = 20m }
            };

            // Test 1: Weight Validation (Must equal 100%)
            bool weightsValid = engine.ValidateComponentWeights(components, out string weightError);
            Assert("Components Weight Sum Validation (100%)", weightsValid, $"Error: {weightError}");

            // Test 2: Standard Student Calculation (Rahul Verma)
            // Test 1: 27/30 -> (27/30)*15 = 13.5
            // Test 2: 26/30 -> (26/30)*15 = 13.0
            // Assgn:  18/20 -> (18/20)*10 = 9.0
            // Viva:   19/20 -> (19/20)*10 = 9.5
            // Total: 45.00 / 50.00 (90%, Grade O, Pass)
            var scores1 = new Dictionary<int, decimal> { { 1, 27m }, { 2, 26m }, { 3, 18m }, { 4, 19m } };
            var result1 = engine.Calculate(50m, 20m, components, scores1);

            Assert("Rahul Scored Total", result1.CalculatedTotal == 45.00m, $"Expected 45.00, Got {result1.CalculatedTotal}");
            Assert("Rahul Percentage", result1.Percentage == 90.00m, $"Expected 90.00%, Got {result1.Percentage}%");
            Assert("Rahul Grade O", result1.Grade == "O", $"Expected O, Got {result1.Grade}");
            Assert("Rahul Pass Status", result1.IsPass == true, $"Expected True, Got {result1.IsPass}");

            // Test 3: Failing Student Calculation (Pooja Rao)
            // Test 1: 10/30 -> 5.0
            // Test 2: 9/30  -> 4.5
            // Assgn:  8/20  -> 4.0
            // Viva:   9/20  -> 4.5
            // Total: 18.00 / 50.00 (36%, Grade F, Fail)
            var scoresFail = new Dictionary<int, decimal> { { 1, 10m }, { 2, 9m }, { 3, 8m }, { 4, 9m } };
            var resultFail = engine.Calculate(50m, 20m, components, scoresFail);

            Assert("Pooja Scored Total", resultFail.CalculatedTotal == 18.00m, $"Expected 18.00, Got {resultFail.CalculatedTotal}");
            Assert("Pooja Percentage", resultFail.Percentage == 36.00m, $"Expected 36.00%, Got {resultFail.Percentage}%");
            Assert("Pooja Grade F", resultFail.Grade == "F", $"Expected F, Got {resultFail.Grade}");
            Assert("Pooja Fail Status", resultFail.IsPass == false, $"Expected False, Got {resultFail.IsPass}");

            // Test 4: Clamping Ceiling
            var scoresOverflow = new Dictionary<int, decimal> { { 1, 100m }, { 2, 100m }, { 3, 100m }, { 4, 100m } };
            var resultOverflow = engine.Calculate(50m, 20m, components, scoresOverflow);
            Assert("Max Score Capped at 50", resultOverflow.CalculatedTotal == 50.00m, $"Expected 50.00, Got {resultOverflow.CalculatedTotal}");

            // Test 5: All Grade Boundaries
            Assert("Grade O (90%)", engine.DetermineGrade(90m) == "O", "90% should be O");
            Assert("Grade A+ (80%)", engine.DetermineGrade(80m) == "A+", "80% should be A+");
            Assert("Grade A (70%)", engine.DetermineGrade(70m) == "A", "70% should be A");
            Assert("Grade B+ (60%)", engine.DetermineGrade(60m) == "B+", "60% should be B+");
            Assert("Grade B (50%)", engine.DetermineGrade(50m) == "B", "50% should be B");
            Assert("Grade C (40%)", engine.DetermineGrade(40m) == "C", "40% should be C");
            Assert("Grade F (39.9%)", engine.DetermineGrade(39.9m) == "F", "39.9% should be F");

            Console.WriteLine("=================================================");
            Console.WriteLine("ALL 12 CALCULATION ENGINE ASSERTIONS PASSED!");
            Console.WriteLine("=================================================");
        }

        private static void Assert(string name, bool condition, string detail)
        {
            if (condition)
            {
                Console.WriteLine($"[PASS] {name}");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[FAIL] {name} - {detail}");
                Console.ResetColor();
                Environment.Exit(1);
            }
        }
    }
}
