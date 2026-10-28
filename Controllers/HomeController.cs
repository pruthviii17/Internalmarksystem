using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InternalMarksSystem.Models;
using InternalMarksSystem.Data;
using InternalMarksSystem.Services;

namespace InternalMarksSystem.Controllers
{
    public class DiagnosticItem
    {
        public string TestCategory { get; set; } = string.Empty;
        public string TestName { get; set; } = string.Empty;
        public bool Passed { get; set; }
        public string Expected { get; set; } = string.Empty;
        public string Actual { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }

    public class DiagnosticsViewModel
    {
        public bool AllPassed { get; set; }
        public int TotalTests { get; set; }
        public int PassedTests { get; set; }
        public int FailedTests { get; set; }
        public List<DiagnosticItem> Items { get; set; } = new();
    }

    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IMarksCalculationEngine _calculationEngine;

        public HomeController(ApplicationDbContext context, IMarksCalculationEngine calculationEngine)
        {
            _context = context;
            _calculationEngine = calculationEngine;
        }

        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Dashboard");
            }
            return RedirectToAction("Login", "Account");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        // System self-test and verification diagnostic suite
        [HttpGet]
        public async Task<IActionResult> Diagnostics()
        {
            var model = new DiagnosticsViewModel();

            // 1. Test Calculation Engine: Standard Weighted Calculation
            var components = new List<AssessmentComponent>
            {
                new() { Id = 1, Name = "Internal Test 1", MaxMarks = 30m, WeightPercentage = 30m },
                new() { Id = 2, Name = "Internal Test 2", MaxMarks = 30m, WeightPercentage = 30m },
                new() { Id = 3, Name = "Assignments", MaxMarks = 20m, WeightPercentage = 20m },
                new() { Id = 4, Name = "Practical / Viva", MaxMarks = 20m, WeightPercentage = 20m }
            };

            var sampleScores = new Dictionary<int, decimal>
            {
                { 1, 27m }, // (27/30)*15 = 13.5
                { 2, 26m }, // (26/30)*15 = 13.0
                { 3, 18m }, // (18/20)*10 = 9.0
                { 4, 19m }  // (19/20)*10 = 9.5  -> Sum = 45.00
            };

            var calcResult = _calculationEngine.Calculate(50m, 20m, components, sampleScores);

            model.Items.Add(new DiagnosticItem
            {
                TestCategory = "Marks Engine",
                TestName = "Weighted Total Calculation",
                Expected = "45.00 / 50.00",
                Actual = $"{calcResult.CalculatedTotal:0.00} / {calcResult.MaxTotal:0.00}",
                Passed = calcResult.CalculatedTotal == 45.00m,
                Notes = "Validates component weight normalization and proportional score summation."
            });

            model.Items.Add(new DiagnosticItem
            {
                TestCategory = "Marks Engine",
                TestName = "Percentage Calculation",
                Expected = "90.00%",
                Actual = $"{calcResult.Percentage:0.00}%",
                Passed = calcResult.Percentage == 90.00m,
                Notes = "(CalculatedTotal / MaxTotal) * 100"
            });

            model.Items.Add(new DiagnosticItem
            {
                TestCategory = "Marks Engine",
                TestName = "Grade Assignment (Outstanding >= 90%)",
                Expected = "O",
                Actual = calcResult.Grade,
                Passed = calcResult.Grade == "O",
                Notes = "Verifies configurable grade boundary mapping."
            });

            model.Items.Add(new DiagnosticItem
            {
                TestCategory = "Marks Engine",
                TestName = "Pass/Fail Threshold Determination",
                Expected = "True (Pass)",
                Actual = $"{calcResult.IsPass} ({(calcResult.IsPass ? "Pass" : "Fail")})",
                Passed = calcResult.IsPass,
                Notes = "Score >= PassingMarks (45.00 >= 20.00)"
            });

            // Clamping Test
            var overflowScores = new Dictionary<int, decimal> { { 1, 999m }, { 2, 999m }, { 3, 999m }, { 4, 999m } };
            var overflowCalc = _calculationEngine.Calculate(50m, 20m, components, overflowScores);
            model.Items.Add(new DiagnosticItem
            {
                TestCategory = "Marks Engine",
                TestName = "Score Clamping Ceiling",
                Expected = "<= 50.00",
                Actual = overflowCalc.CalculatedTotal.ToString("0.00"),
                Passed = overflowCalc.CalculatedTotal <= 50.00m,
                Notes = "Ensures no calculation or overflow can exceed maximum configured internal marks."
            });

            // 2. Database & Data Model Integrity Tests
            int deptCount = await _context.Departments.CountAsync();
            model.Items.Add(new DiagnosticItem
            {
                TestCategory = "Database & Seeding",
                TestName = "Departments Table Seeded",
                Expected = ">= 3",
                Actual = deptCount.ToString(),
                Passed = deptCount >= 3,
                Notes = "Computer Science, Information Technology, Electronics & Communication"
            });

            int semCount = await _context.Semesters.CountAsync();
            model.Items.Add(new DiagnosticItem
            {
                TestCategory = "Database & Seeding",
                TestName = "Semesters Table Seeded",
                Expected = "8",
                Actual = semCount.ToString(),
                Passed = semCount >= 8,
                Notes = "Semesters 1 through 8 with Academic Year 2024-2025"
            });

            int subCount = await _context.Subjects.CountAsync();
            model.Items.Add(new DiagnosticItem
            {
                TestCategory = "Database & Seeding",
                TestName = "Subjects Table Seeded",
                Expected = ">= 4",
                Actual = subCount.ToString(),
                Passed = subCount >= 4,
                Notes = "CS501 (DBMS), CS502 (OS), CS503 (Web Tech), IT501 (Software Eng)"
            });

            int studentCount = await _context.Students.CountAsync();
            model.Items.Add(new DiagnosticItem
            {
                TestCategory = "Database & Seeding",
                TestName = "Enrolled Students Seeded",
                Expected = ">= 4",
                Actual = studentCount.ToString(),
                Passed = studentCount >= 4,
                Notes = "Students linked with ApplicationUser authentication accounts"
            });

            int facultyCount = await _context.FacultyProfiles.CountAsync();
            model.Items.Add(new DiagnosticItem
            {
                TestCategory = "Database & Seeding",
                TestName = "Faculty Profiles Seeded",
                Expected = ">= 2",
                Actual = facultyCount.ToString(),
                Passed = facultyCount >= 2,
                Notes = "Prof. Rajesh Sharma and Dr. Ananya Patel"
            });

            // 3. Workflow State Machine Tests
            bool hasDraft = await _context.InternalMarks.AnyAsync(m => m.Status == MarkStatus.Draft);
            bool hasSubmitted = await _context.InternalMarks.AnyAsync(m => m.Status == MarkStatus.Submitted);
            bool hasPublished = await _context.InternalMarks.AnyAsync(m => m.Status == MarkStatus.Published);

            model.Items.Add(new DiagnosticItem
            {
                TestCategory = "Workflow State Machine",
                TestName = "Multi-State Marks Records Present",
                Expected = "Draft, Submitted, Published exist",
                Actual = $"Draft: {hasDraft}, Submitted: {hasSubmitted}, Published: {hasPublished}",
                Passed = hasDraft && hasSubmitted && hasPublished,
                Notes = "Verifies support for complete 4-state approval & publication workflow."
            });

            int auditCount = await _context.AuditLogs.CountAsync();
            model.Items.Add(new DiagnosticItem
            {
                TestCategory = "Security & Governance",
                TestName = "Audit Trail Logging Active",
                Expected = ">= 5 entries",
                Actual = $"{auditCount} entries",
                Passed = auditCount >= 5,
                Notes = "Immutable tracking of system actions, timestamps (UTC), and users."
            });

            model.TotalTests = model.Items.Count;
            model.PassedTests = model.Items.Count(i => i.Passed);
            model.FailedTests = model.Items.Count(i => !i.Passed);
            model.AllPassed = model.FailedTests == 0;

            return View(model);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
