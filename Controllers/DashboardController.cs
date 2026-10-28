using InternalMarksSystem.Data;
using InternalMarksSystem.Models;
using InternalMarksSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InternalMarksSystem.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var roles = await _userManager.GetRolesAsync(user);
            string primaryRole = roles.FirstOrDefault() ?? user.UserRole ?? "Student";

            var model = new DashboardViewModel
            {
                UserRole = primaryRole,
                UserName = user.Email ?? "User",
                UserFullName = user.FullName ?? user.UserName ?? "User",
                TotalStudents = await _context.Students.CountAsync(),
                TotalSubjects = await _context.Subjects.CountAsync(),
                TotalFaculty = await _context.FacultyProfiles.CountAsync(),
                TotalMarksRecords = await _context.InternalMarks.CountAsync(),
                PendingApprovals = await _context.InternalMarks.Where(m => m.Status == MarkStatus.Submitted).CountAsync(),
                PublishedResults = await _context.InternalMarks.Where(m => m.Status == MarkStatus.Published).CountAsync(),
                RecentActivities = await _context.AuditLogs.OrderByDescending(a => a.TimestampUtc).Take(7).ToListAsync()
            };

            // Global Chart Data (Subject Averages and Pass/Fail counts)
            var subjects = await _context.Subjects
                .Include(s => s.InternalMarks)
                .Where(s => s.InternalMarks.Any(m => m.Status == MarkStatus.Published))
                .ToListAsync();

            foreach (var sub in subjects)
            {
                var pubMarks = sub.InternalMarks.Where(m => m.Status == MarkStatus.Published).ToList();
                if (pubMarks.Any())
                {
                    model.SubjectNames.Add(sub.SubjectCode);
                    model.SubjectAverages.Add(Math.Round(pubMarks.Average(m => m.CalculatedTotal), 2));
                }
            }

            var allPublished = await _context.InternalMarks.Where(m => m.Status == MarkStatus.Published).ToListAsync();
            model.PassCount = allPublished.Count(m => m.IsPass);
            model.FailCount = allPublished.Count(m => !m.IsPass);

            // Grade distribution
            foreach (var grade in new[] { "O", "A+", "A", "B+", "B", "C", "F" })
            {
                model.GradeDistribution[grade] = allPublished.Count(m => m.Grade == grade);
            }

            // Role-specific population
            if (primaryRole == "Faculty")
            {
                var facultyProfile = await _context.FacultyProfiles
                    .FirstOrDefaultAsync(f => f.ApplicationUserId == user.Id || f.Email == user.Email);

                if (facultyProfile != null)
                {
                    var assignedSubjectIds = await _context.FacultySubjectAssignments
                        .Where(a => a.FacultyProfileId == facultyProfile.Id)
                        .Select(a => a.SubjectId)
                        .ToListAsync();

                    model.AssignedSubjects = await _context.Subjects
                        .Include(s => s.Department)
                        .Include(s => s.Semester)
                        .Include(s => s.InternalMarks)
                        .Where(s => assignedSubjectIds.Contains(s.Id))
                        .ToListAsync();

                    model.MyDraftMarksCount = await _context.InternalMarks
                        .Where(m => assignedSubjectIds.Contains(m.SubjectId) && m.Status == MarkStatus.Draft)
                        .CountAsync();

                    model.MySubmittedMarksCount = await _context.InternalMarks
                        .Where(m => assignedSubjectIds.Contains(m.SubjectId) && m.Status == MarkStatus.Submitted)
                        .CountAsync();
                }
            }
            else if (primaryRole == "Student")
            {
                var studentProfile = await _context.Students
                    .Include(s => s.Department)
                    .Include(s => s.Semester)
                    .FirstOrDefaultAsync(s => s.ApplicationUserId == user.Id || s.Email == user.Email);

                model.StudentProfile = studentProfile;

                if (studentProfile != null)
                {
                    var studentMarks = await _context.InternalMarks
                        .Include(m => m.Subject)
                        .Include(m => m.Semester)
                        .Include(m => m.Details)
                            .ThenInclude(d => d.AssessmentComponent)
                        .Where(m => m.StudentId == studentProfile.Id && m.Status == MarkStatus.Published)
                        .ToListAsync();

                    model.StudentPublishedMarks = studentMarks;
                    model.StudentTotalSubjectsCount = studentMarks.Count;
                    model.StudentPassedSubjectsCount = studentMarks.Count(m => m.IsPass);

                    if (studentMarks.Any())
                    {
                        decimal totalObtained = studentMarks.Sum(m => m.CalculatedTotal);
                        decimal totalMax = studentMarks.Sum(m => m.MaxTotal);
                        model.StudentOverallPercentage = totalMax > 0 
                            ? Math.Round((totalObtained / totalMax) * 100m, 2) 
                            : 0m;
                    }
                }
            }

            return View(model);
        }
    }
}
