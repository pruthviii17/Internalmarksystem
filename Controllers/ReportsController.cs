using InternalMarksSystem.Data;
using InternalMarksSystem.Models;
using InternalMarksSystem.Services;
using InternalMarksSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace InternalMarksSystem.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IMarksCalculationEngine _calculationEngine;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReportsController(
            ApplicationDbContext context,
            IMarksCalculationEngine calculationEngine,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _calculationEngine = calculationEngine;
            _userManager = userManager;
        }

        // GET: Reports
        public async Task<IActionResult> Index(int? departmentId, int? semesterId, int? subjectId, string? searchEnrollment)
        {
            ViewBag.Departments = new SelectList(await _context.Departments.OrderBy(d => d.Name).ToListAsync(), "Id", "Name", departmentId);
            ViewBag.Semesters = new SelectList(await _context.Semesters.OrderBy(s => s.SemesterNumber).ToListAsync(), "Id", "Name", semesterId);
            ViewBag.Subjects = new SelectList(await _context.Subjects.OrderBy(s => s.SubjectCode).ToListAsync(), "Id", "SubjectName", subjectId);

            ViewBag.CurrentDept = departmentId;
            ViewBag.CurrentSem = semesterId;
            ViewBag.CurrentSubject = subjectId;
            ViewBag.SearchEnrollment = searchEnrollment;

            // List available subjects with published marks
            var query = _context.Subjects
                .Include(s => s.Department)
                .Include(s => s.Semester)
                .Include(s => s.InternalMarks)
                .AsQueryable();

            if (departmentId.HasValue && departmentId.Value > 0)
                query = query.Where(s => s.DepartmentId == departmentId.Value);

            if (semesterId.HasValue && semesterId.Value > 0)
                query = query.Where(s => s.SemesterId == semesterId.Value);

            if (subjectId.HasValue && subjectId.Value > 0)
                query = query.Where(s => s.Id == subjectId.Value);

            var subjects = await query.ToListAsync();

            // Also find student if searchEnrollment is provided
            if (!string.IsNullOrWhiteSpace(searchEnrollment))
            {
                var student = await _context.Students
                    .FirstOrDefaultAsync(s => s.EnrollmentNumber.ToLower() == searchEnrollment.Trim().ToLower());
                ViewBag.MatchedStudent = student;
            }

            return View(subjects);
        }

        // GET: Reports/StudentReport?studentId=1&enrollmentNo=2022CSE001
        public async Task<IActionResult> StudentReport(int? studentId, string? enrollmentNo)
        {
            Student? student = null;

            // If user is Student, enforce viewing only their own record
            if (User.IsInRole("Student"))
            {
                var user = await _userManager.GetUserAsync(User);
                student = await _context.Students
                    .Include(s => s.Department)
                    .Include(s => s.Semester)
                    .FirstOrDefaultAsync(s => s.ApplicationUserId == user!.Id || s.Email == user!.Email);
            }
            else
            {
                if (studentId.HasValue)
                {
                    student = await _context.Students
                        .Include(s => s.Department)
                        .Include(s => s.Semester)
                        .FirstOrDefaultAsync(s => s.Id == studentId.Value);
                }
                else if (!string.IsNullOrWhiteSpace(enrollmentNo))
                {
                    student = await _context.Students
                        .Include(s => s.Department)
                        .Include(s => s.Semester)
                        .FirstOrDefaultAsync(s => s.EnrollmentNumber.ToLower() == enrollmentNo.Trim().ToLower());
                }
            }

            if (student == null)
            {
                TempData["ErrorMessage"] = "Student not found.";
                return RedirectToAction(nameof(Index));
            }

            var marksQuery = _context.InternalMarks
                .Include(m => m.Subject)
                .Include(m => m.Semester)
                .Include(m => m.Details)
                    .ThenInclude(d => d.AssessmentComponent)
                .Where(m => m.StudentId == student.Id);

            // Students only see published marks; Admin/Faculty can see all
            if (User.IsInRole("Student"))
            {
                marksQuery = marksQuery.Where(m => m.Status == MarkStatus.Published);
            }

            var marksList = await marksQuery.OrderBy(m => m.Subject!.SubjectCode).ToListAsync();

            var report = new StudentReportViewModel
            {
                Student = student,
                Semester = student.Semester,
                TotalCredits = marksList.Sum(m => m.Subject?.Credits ?? 0),
                TotalMaxMarks = marksList.Sum(m => m.MaxTotal),
                TotalObtainedMarks = marksList.Sum(m => m.CalculatedTotal)
            };

            if (report.TotalMaxMarks > 0)
            {
                report.OverallPercentage = Math.Round((report.TotalObtainedMarks / report.TotalMaxMarks) * 100m, 2);
            }

            report.OverallResult = marksList.Any(m => !m.IsPass) ? "Fail" : "Pass";

            foreach (var m in marksList)
            {
                report.Results.Add(new StudentSubjectResultItem
                {
                    SubjectCode = m.Subject?.SubjectCode ?? "",
                    SubjectName = m.Subject?.SubjectName ?? "",
                    Credits = m.Subject?.Credits ?? 0,
                    MaxMarks = m.MaxTotal,
                    PassingMarks = m.Subject?.PassingMarks ?? 20m,
                    ObtainedMarks = m.CalculatedTotal,
                    Percentage = m.Percentage,
                    Grade = m.Grade,
                    IsPass = m.IsPass,
                    ComponentDetails = m.Details.OrderBy(d => d.AssessmentComponent?.DisplayOrder).ToList()
                });
            }

            return View(report);
        }

        // GET: Reports/SubjectReport?subjectId=1
        public async Task<IActionResult> SubjectReport(int? subjectId)
        {
            if (!subjectId.HasValue) return NotFound();

            var subject = await _context.Subjects
                .Include(s => s.Department)
                .Include(s => s.Semester)
                .Include(s => s.AssessmentComponents.OrderBy(c => c.DisplayOrder))
                .FirstOrDefaultAsync(s => s.Id == subjectId.Value);

            if (subject == null) return NotFound();

            var marks = await _context.InternalMarks
                .Include(m => m.Student)
                .Include(m => m.Details)
                    .ThenInclude(d => d.AssessmentComponent)
                .Where(m => m.SubjectId == subject.Id && (m.Status == MarkStatus.Published || m.Status == MarkStatus.Approved))
                .OrderByDescending(m => m.CalculatedTotal)
                .ToListAsync();

            var stats = _calculationEngine.CalculateSubjectStatistics(subject, marks);

            var model = new SubjectReportViewModel
            {
                Subject = subject,
                Semester = subject.Semester,
                Statistics = stats,
                MarksList = marks
            };

            return View(model);
        }

        // GET: Reports/SemesterReport?semesterId=5
        public async Task<IActionResult> SemesterReport(int? semesterId)
        {
            var semId = semesterId ?? 5;
            var semester = await _context.Semesters.FirstOrDefaultAsync(s => s.Id == semId)
                ?? await _context.Semesters.FirstOrDefaultAsync(s => s.SemesterNumber == 5);

            if (semester == null) return NotFound();

            var marks = await _context.InternalMarks
                .Include(m => m.Subject)
                .Include(m => m.Student)
                .Where(m => m.SemesterId == semester.Id && (m.Status == MarkStatus.Published || m.Status == MarkStatus.Approved))
                .ToListAsync();

            var stats = _calculationEngine.CalculateSemesterStatistics(semester, marks);

            ViewBag.Semester = semester;
            ViewBag.Semesters = new SelectList(await _context.Semesters.OrderBy(s => s.SemesterNumber).ToListAsync(), "Id", "Name", semester.Id);
            ViewBag.Stats = stats;
            ViewBag.SubjectSummaries = marks.GroupBy(m => m.Subject)
                .Select(g => _calculationEngine.CalculateSubjectStatistics(g.Key!, g.ToList()))
                .ToList();

            return View(marks);
        }

        // GET: Reports/ExportCsv?reportType=subject&id=1
        public async Task<IActionResult> ExportCsv(string reportType, int id)
        {
            var sb = new StringBuilder();

            if (string.Equals(reportType, "subject", StringComparison.OrdinalIgnoreCase))
            {
                var subject = await _context.Subjects
                    .Include(s => s.AssessmentComponents.OrderBy(c => c.DisplayOrder))
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (subject == null) return NotFound();

                var marks = await _context.InternalMarks
                    .Include(m => m.Student)
                    .Include(m => m.Details)
                        .ThenInclude(d => d.AssessmentComponent)
                    .Where(m => m.SubjectId == id)
                    .OrderBy(m => m.Student!.EnrollmentNumber)
                    .ToListAsync();

                sb.Append("Enrollment No,Student Name,");
                foreach (var comp in subject.AssessmentComponents)
                {
                    sb.Append($"\"{comp.Name} (Max {comp.MaxMarks:0.#})\",");
                }
                sb.AppendLine($"\"Total (Max {subject.MaxInternalMarks:0.#})\",Percentage,Grade,Pass/Fail Status,Workflow Status");

                foreach (var m in marks)
                {
                    sb.Append($"\"{m.Student?.EnrollmentNumber}\",\"{m.Student?.FullName}\",");
                    foreach (var comp in subject.AssessmentComponents)
                    {
                        var det = m.Details.FirstOrDefault(d => d.AssessmentComponentId == comp.Id);
                        sb.Append($"{det?.MarksObtained ?? 0m:0.##},");
                    }
                    sb.AppendLine($"{m.CalculatedTotal:0.##},{m.Percentage:0.##}%,{m.Grade},{(m.IsPass ? "PASS" : "FAIL")},{m.Status}");
                }

                byte[] bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
                return File(bytes, "text/csv", $"{subject.SubjectCode}_Marks_Report.csv");
            }
            else if (string.Equals(reportType, "student", StringComparison.OrdinalIgnoreCase))
            {
                var student = await _context.Students
                    .Include(s => s.Department)
                    .Include(s => s.Semester)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (student == null) return NotFound();

                var marks = await _context.InternalMarks
                    .Include(m => m.Subject)
                    .Where(m => m.StudentId == id && m.Status == MarkStatus.Published)
                    .ToListAsync();

                sb.AppendLine($"Student Marks Report - {student.FullName} ({student.EnrollmentNumber})");
                sb.AppendLine($"Department: {student.Department?.Name}, Semester: {student.Semester?.Name}");
                sb.AppendLine();
                sb.AppendLine("Subject Code,Subject Name,Credits,Max Marks,Obtained Marks,Percentage,Grade,Status");

                foreach (var m in marks)
                {
                    sb.AppendLine($"\"{m.Subject?.SubjectCode}\",\"{m.Subject?.SubjectName}\",{m.Subject?.Credits},{m.MaxTotal:0.##},{m.CalculatedTotal:0.##},{m.Percentage:0.##}%,{m.Grade},{(m.IsPass ? "PASS" : "FAIL")}");
                }

                byte[] bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
                return File(bytes, "text/csv", $"{student.EnrollmentNumber}_Marksheet.csv");
            }

            return BadRequest();
        }
    }
}
