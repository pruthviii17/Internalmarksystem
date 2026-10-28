using InternalMarksSystem.Data;
using InternalMarksSystem.Models;
using InternalMarksSystem.Services;
using InternalMarksSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InternalMarksSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ApprovalController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditService _auditService;

        public ApprovalController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IAuditService auditService)
        {
            _context = context;
            _userManager = userManager;
            _auditService = auditService;
        }

        // GET: Approval
        public async Task<IActionResult> Index()
        {
            var subjects = await _context.Subjects
                .Include(s => s.Department)
                .Include(s => s.Semester)
                .Include(s => s.FacultyAssignments)
                    .ThenInclude(fa => fa.FacultyProfile)
                .Include(s => s.InternalMarks)
                .OrderBy(s => s.SubjectCode)
                .ToListAsync();

            var groups = new List<ApprovalSubmissionGroupViewModel>();

            foreach (var s in subjects)
            {
                var marks = s.InternalMarks.ToList();
                var facultyName = s.FacultyAssignments.FirstOrDefault()?.FacultyProfile?.FullName ?? "Not Assigned";

                groups.Add(new ApprovalSubmissionGroupViewModel
                {
                    SubjectId = s.Id,
                    SubjectCode = s.SubjectCode,
                    SubjectName = s.SubjectName,
                    DepartmentName = s.Department?.Name ?? "",
                    SemesterId = s.SemesterId,
                    SemesterName = s.Semester?.Name ?? $"Sem {s.SemesterId}",
                    AcademicYear = s.Semester?.AcademicYear ?? "2024-2025",
                    FacultyName = facultyName,
                    TotalRecords = marks.Count,
                    DraftCount = marks.Count(m => m.Status == MarkStatus.Draft),
                    SubmittedCount = marks.Count(m => m.Status == MarkStatus.Submitted),
                    ApprovedCount = marks.Count(m => m.Status == MarkStatus.Approved),
                    PublishedCount = marks.Count(m => m.Status == MarkStatus.Published),
                    RejectedCount = marks.Count(m => m.Status == MarkStatus.Rejected),
                    LatestSubmittedAt = marks.Where(m => m.Status == MarkStatus.Submitted).Max(m => m.SubmittedAtUtc)
                });
            }

            return View(groups);
        }

        // GET: Approval/Review?subjectId=1&semesterId=5
        public async Task<IActionResult> Review(int subjectId, int semesterId)
        {
            var subject = await _context.Subjects
                .Include(s => s.Department)
                .Include(s => s.Semester)
                .Include(s => s.AssessmentComponents.OrderBy(c => c.DisplayOrder))
                .FirstOrDefaultAsync(s => s.Id == subjectId);

            if (subject == null) return NotFound();

            var marks = await _context.InternalMarks
                .Include(m => m.Student)
                .Include(m => m.Details)
                    .ThenInclude(d => d.AssessmentComponent)
                .Where(m => m.SubjectId == subjectId && m.SemesterId == semesterId)
                .OrderBy(m => m.Student!.EnrollmentNumber)
                .ToListAsync();

            ViewBag.Subject = subject;
            ViewBag.SemesterId = semesterId;

            return View(marks);
        }

        // POST: Approval/Approve
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int subjectId, int semesterId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var marks = await _context.InternalMarks
                .Where(m => m.SubjectId == subjectId && m.SemesterId == semesterId && m.Status == MarkStatus.Submitted)
                .ToListAsync();

            if (!marks.Any())
            {
                TempData["ErrorMessage"] = "No submitted marks found to approve for this subject.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var m in marks)
            {
                m.Status = MarkStatus.Approved;
                m.ApprovedByUserId = user.Id;
                m.ApprovedAtUtc = DateTime.UtcNow;
                m.RejectionReason = null;
            }

            await _context.SaveChangesAsync();

            var subject = await _context.Subjects.FindAsync(subjectId);
            string subjectCode = subject?.SubjectCode ?? subjectId.ToString();

            await _auditService.LogAsync("Approve Marks", "Subject", subjectId.ToString(),
                $"Approved {marks.Count} internal marks records for {subjectCode}, Semester {semesterId}.");

            TempData["SuccessMessage"] = $"Marks for subject '{subjectCode}' successfully approved! Ready for publication.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Approval/Reject
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(ApprovalActionViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            if (string.IsNullOrWhiteSpace(model.Reason))
            {
                TempData["ErrorMessage"] = "A rejection reason is strictly required when rejecting marks.";
                return RedirectToAction(nameof(Review), new { subjectId = model.SubjectId, semesterId = model.SemesterId });
            }

            var marks = await _context.InternalMarks
                .Where(m => m.SubjectId == model.SubjectId && m.SemesterId == model.SemesterId && m.Status == MarkStatus.Submitted)
                .ToListAsync();

            if (!marks.Any())
            {
                TempData["ErrorMessage"] = "No submitted marks found to reject.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var m in marks)
            {
                m.Status = MarkStatus.Rejected;
                m.RejectionReason = model.Reason.Trim();
                m.UpdatedAtUtc = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            var subject = await _context.Subjects.FindAsync(model.SubjectId);
            string subjectCode = subject?.SubjectCode ?? model.SubjectId.ToString();

            await _auditService.LogAsync("Reject Marks", "Subject", model.SubjectId.ToString(),
                $"Rejected marks for {subjectCode}. Reason: {model.Reason}");

            TempData["SuccessMessage"] = $"Marks for '{subjectCode}' were rejected and sent back to faculty with reason: \"{model.Reason}\"";
            return RedirectToAction(nameof(Index));
        }

        // POST: Approval/Publish
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Publish(int subjectId, int semesterId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var marks = await _context.InternalMarks
                .Where(m => m.SubjectId == subjectId && m.SemesterId == semesterId && m.Status == MarkStatus.Approved)
                .ToListAsync();

            if (!marks.Any())
            {
                TempData["ErrorMessage"] = "Only approved marks can be published. No approved marks found.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var m in marks)
            {
                m.Status = MarkStatus.Published;
                m.PublishedByUserId = user.Id;
                m.PublishedAtUtc = DateTime.UtcNow;
            }

            // Create publication record
            var pubRecord = new ResultPublicationRecord
            {
                SubjectId = subjectId,
                SemesterId = semesterId,
                AcademicYear = "2024-2025",
                PublishedByUserId = user.Id,
                PublishedAtUtc = DateTime.UtcNow,
                TotalStudentsCount = marks.Count,
                Notes = $"Published results for {marks.Count} students."
            };

            _context.ResultPublicationRecords.Add(pubRecord);
            await _context.SaveChangesAsync();

            var subject = await _context.Subjects.FindAsync(subjectId);
            string subjectCode = subject?.SubjectCode ?? subjectId.ToString();

            await _auditService.LogAsync("Publish Marks", "Subject", subjectId.ToString(),
                $"Published internal marks for {subjectCode}. Students can now view their grades.");

            TempData["SuccessMessage"] = $"Results for '{subjectCode}' published successfully! Students can now view their marks.";
            return RedirectToAction(nameof(Index));
        }
    }
}
