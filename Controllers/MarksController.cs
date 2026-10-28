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
    [Authorize]
    public class MarksController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IMarksCalculationEngine _calculationEngine;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditService _auditService;

        public MarksController(
            ApplicationDbContext context,
            IMarksCalculationEngine calculationEngine,
            UserManager<ApplicationUser> userManager,
            IAuditService auditService)
        {
            _context = context;
            _calculationEngine = calculationEngine;
            _userManager = userManager;
            _auditService = auditService;
        }

        // GET: Marks/Enter?subjectId=1&semesterId=5
        [Authorize(Roles = "Admin,Faculty")]
        public async Task<IActionResult> Enter(int subjectId, int? semesterId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var subject = await _context.Subjects
                .Include(s => s.Department)
                .Include(s => s.Semester)
                .Include(s => s.AssessmentComponents.OrderBy(c => c.DisplayOrder))
                .FirstOrDefaultAsync(s => s.Id == subjectId);

            if (subject == null)
            {
                TempData["ErrorMessage"] = "Subject not found.";
                return RedirectToAction("Index", "Dashboard");
            }

            int semId = semesterId ?? subject.SemesterId;

            // Faculty permission check: Restrict faculty to assigned subjects
            if (User.IsInRole("Faculty"))
            {
                var facultyProfile = await _context.FacultyProfiles
                    .FirstOrDefaultAsync(f => f.ApplicationUserId == user.Id || f.Email == user.Email);

                if (facultyProfile == null)
                {
                    TempData["ErrorMessage"] = "Faculty profile not found.";
                    return RedirectToAction("Index", "Dashboard");
                }

                bool isAssigned = await _context.FacultySubjectAssignments
                    .AnyAsync(fa => fa.FacultyProfileId == facultyProfile.Id && fa.SubjectId == subjectId);

                if (!isAssigned)
                {
                    TempData["ErrorMessage"] = "Access restricted: You are not assigned to evaluate marks for this subject.";
                    return RedirectToAction("Index", "Dashboard");
                }
            }

            // Ensure components are configured
            if (!subject.AssessmentComponents.Any())
            {
                TempData["ErrorMessage"] = "This subject has no assessment components configured yet. Please configure components first.";
                return RedirectToAction("ConfigureComponents", "Subjects", new { id = subjectId });
            }

            // Fetch enrolled students for this department and semester
            var students = await _context.Students
                .Where(s => s.DepartmentId == subject.DepartmentId && s.SemesterId == semId)
                .OrderBy(s => s.EnrollmentNumber)
                .ToListAsync();

            // Fetch existing marks for this subject and semester
            var existingMarks = await _context.InternalMarks
                .Include(m => m.Details)
                .Where(m => m.SubjectId == subjectId && m.SemesterId == semId)
                .ToListAsync();

            // Determine if marks are read-only for faculty
            bool anySubmitted = existingMarks.Any(m => m.Status == MarkStatus.Submitted);
            bool anyApproved = existingMarks.Any(m => m.Status == MarkStatus.Approved);
            bool anyPublished = existingMarks.Any(m => m.Status == MarkStatus.Published);
            bool anyRejected = existingMarks.Any(m => m.Status == MarkStatus.Rejected);

            bool isReadOnly = false;
            string statusSummary = "Draft Mode - Marks can be edited and saved.";

            if (User.IsInRole("Faculty"))
            {
                if (anyPublished)
                {
                    isReadOnly = true;
                    statusSummary = "Results Published - Marks are locked and cannot be edited.";
                }
                else if (anyApproved)
                {
                    isReadOnly = true;
                    statusSummary = "Marks Approved by HOD - Awaiting publication. Editing locked.";
                }
                else if (anySubmitted)
                {
                    isReadOnly = true;
                    statusSummary = "Marks Submitted for Approval - Awaiting HOD review. Editing is locked.";
                }
                else if (anyRejected)
                {
                    isReadOnly = false;
                    var firstRejected = existingMarks.FirstOrDefault(m => m.Status == MarkStatus.Rejected);
                    statusSummary = $"Marks Rejected: \"{firstRejected?.RejectionReason}\". Please correct and resubmit.";
                }
            }

            var model = new MarksEntryViewModel
            {
                SubjectId = subject.Id,
                SubjectCode = subject.SubjectCode,
                SubjectName = subject.SubjectName,
                DepartmentName = subject.Department?.Name ?? string.Empty,
                SemesterId = semId,
                SemesterName = subject.Semester?.Name ?? $"Semester {semId}",
                AcademicYear = "2024-2025",
                MaxInternalMarks = subject.MaxInternalMarks,
                PassingMarks = subject.PassingMarks,
                IsReadOnly = isReadOnly,
                StatusSummary = statusSummary,
                Components = subject.AssessmentComponents.ToList()
            };

            foreach (var student in students)
            {
                var existingMark = existingMarks.FirstOrDefault(m => m.StudentId == student.Id);
                var row = new StudentMarkRowViewModel
                {
                    StudentId = student.Id,
                    EnrollmentNumber = student.EnrollmentNumber,
                    StudentName = student.FullName,
                    InternalMarkId = existingMark?.Id,
                    Status = existingMark?.Status ?? MarkStatus.Draft,
                    RejectionReason = existingMark?.RejectionReason
                };

                var scoreDict = new Dictionary<int, decimal>();
                foreach (var comp in model.Components)
                {
                    var detail = existingMark?.Details.FirstOrDefault(d => d.AssessmentComponentId == comp.Id);
                    decimal val = detail?.MarksObtained ?? 0m;
                    row.ComponentMarks[comp.Id] = val;
                    scoreDict[comp.Id] = val;
                }

                if (existingMark != null)
                {
                    row.CalculatedTotal = existingMark.CalculatedTotal;
                    row.Percentage = existingMark.Percentage;
                    row.Grade = existingMark.Grade;
                    row.IsPass = existingMark.IsPass;
                }
                else
                {
                    var calc = _calculationEngine.Calculate(model.MaxInternalMarks, model.PassingMarks, model.Components, scoreDict);
                    row.CalculatedTotal = calc.CalculatedTotal;
                    row.Percentage = calc.Percentage;
                    row.Grade = calc.Grade;
                    row.IsPass = calc.IsPass;
                }

                model.Students.Add(row);
            }

            return View(model);
        }

        // POST: Marks/Save
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Faculty")]
        public async Task<IActionResult> Save(MarksEntryViewModel model, string submitAction)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var subject = await _context.Subjects
                .Include(s => s.AssessmentComponents)
                .FirstOrDefaultAsync(s => s.Id == model.SubjectId);

            if (subject == null)
            {
                TempData["ErrorMessage"] = "Subject not found.";
                return RedirectToAction("Index", "Dashboard");
            }

            // Check faculty assignment restriction
            if (User.IsInRole("Faculty"))
            {
                var facultyProfile = await _context.FacultyProfiles
                    .FirstOrDefaultAsync(f => f.ApplicationUserId == user.Id || f.Email == user.Email);

                if (facultyProfile == null || !await _context.FacultySubjectAssignments.AnyAsync(fa => fa.FacultyProfileId == facultyProfile.Id && fa.SubjectId == model.SubjectId))
                {
                    TempData["ErrorMessage"] = "Unauthorized: You are not assigned to this subject.";
                    return RedirectToAction("Index", "Dashboard");
                }
            }

            var components = subject.AssessmentComponents.ToList();
            bool isSubmitting = string.Equals(submitAction, "submit", StringComparison.OrdinalIgnoreCase);

            // Server-side validation of marks boundaries
            var validationErrors = new List<string>();
            foreach (var studentRow in model.Students)
            {
                foreach (var comp in components)
                {
                    studentRow.ComponentMarks.TryGetValue(comp.Id, out decimal marks);
                    if (marks < 0)
                    {
                        validationErrors.Add($"Student {studentRow.EnrollmentNumber}: Marks for '{comp.Name}' cannot be negative.");
                    }
                    else if (marks > comp.MaxMarks)
                    {
                        validationErrors.Add($"Student {studentRow.EnrollmentNumber}: Marks for '{comp.Name}' ({marks}) exceed maximum allowed ({comp.MaxMarks}).");
                    }
                }
            }

            if (validationErrors.Any())
            {
                foreach (var err in validationErrors.Take(5))
                {
                    ModelState.AddModelError(string.Empty, err);
                }
                TempData["ErrorMessage"] = validationErrors.First();
                return RedirectToAction(nameof(Enter), new { subjectId = model.SubjectId, semesterId = model.SemesterId });
            }

            // Save or Update internal marks
            foreach (var studentRow in model.Students)
            {
                var calc = _calculationEngine.Calculate(subject.MaxInternalMarks, subject.PassingMarks, components, studentRow.ComponentMarks);

                var mark = await _context.InternalMarks
                    .Include(m => m.Details)
                    .FirstOrDefaultAsync(m => m.StudentId == studentRow.StudentId &&
                                              m.SubjectId == model.SubjectId &&
                                              m.SemesterId == model.SemesterId &&
                                              m.AcademicYear == model.AcademicYear);

                if (mark == null)
                {
                    mark = new InternalMark
                    {
                        StudentId = studentRow.StudentId,
                        SubjectId = model.SubjectId,
                        SemesterId = model.SemesterId,
                        AcademicYear = model.AcademicYear,
                        CalculatedTotal = calc.CalculatedTotal,
                        MaxTotal = calc.MaxTotal,
                        Percentage = calc.Percentage,
                        Grade = calc.Grade,
                        IsPass = calc.IsPass,
                        Status = isSubmitting ? MarkStatus.Submitted : MarkStatus.Draft,
                        CreatedAtUtc = DateTime.UtcNow,
                        UpdatedAtUtc = DateTime.UtcNow
                    };

                    if (isSubmitting)
                    {
                        mark.SubmittedByUserId = user.Id;
                        mark.SubmittedAtUtc = DateTime.UtcNow;
                    }

                    _context.InternalMarks.Add(mark);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    // Update existing
                    mark.CalculatedTotal = calc.CalculatedTotal;
                    mark.MaxTotal = calc.MaxTotal;
                    mark.Percentage = calc.Percentage;
                    mark.Grade = calc.Grade;
                    mark.IsPass = calc.IsPass;
                    mark.UpdatedAtUtc = DateTime.UtcNow;

                    if (isSubmitting)
                    {
                        mark.Status = MarkStatus.Submitted;
                        mark.SubmittedByUserId = user.Id;
                        mark.SubmittedAtUtc = DateTime.UtcNow;
                        mark.RejectionReason = null; // Clear previous rejection reason upon submission
                    }
                    else if (mark.Status == MarkStatus.Rejected)
                    {
                        // Editing a rejected record turns it back to draft until re-submitted
                        mark.Status = MarkStatus.Draft;
                    }

                    _context.InternalMarks.Update(mark);
                }

                // Update component details
                foreach (var comp in components)
                {
                    studentRow.ComponentMarks.TryGetValue(comp.Id, out decimal marksObtained);
                    marksObtained = Math.Max(0m, Math.Min(marksObtained, comp.MaxMarks));

                    var detail = mark.Details.FirstOrDefault(d => d.AssessmentComponentId == comp.Id);
                    if (detail == null)
                    {
                        _context.InternalMarkDetails.Add(new InternalMarkDetail
                        {
                            InternalMarkId = mark.Id,
                            AssessmentComponentId = comp.Id,
                            MarksObtained = marksObtained
                        });
                    }
                    else
                    {
                        detail.MarksObtained = marksObtained;
                        _context.InternalMarkDetails.Update(detail);
                    }
                }
            }

            await _context.SaveChangesAsync();

            string actionName = isSubmitting ? "Submit Marks for Approval" : "Save Draft Marks";
            string logDetails = $"{actionName} for {subject.SubjectCode} ({subject.SubjectName}), Semester {model.SemesterId}. Records: {model.Students.Count}.";
            await _auditService.LogAsync(actionName, "InternalMark", subject.Id.ToString(), logDetails);

            if (isSubmitting)
            {
                TempData["SuccessMessage"] = $"Marks for '{subject.SubjectCode} - {subject.SubjectName}' submitted successfully for HOD/Admin approval!";
            }
            else
            {
                TempData["SuccessMessage"] = $"Draft marks for '{subject.SubjectCode} - {subject.SubjectName}' saved successfully.";
            }

            return RedirectToAction(nameof(Enter), new { subjectId = model.SubjectId, semesterId = model.SemesterId });
        }

        // GET: Marks/MyMarks (For logged in Student)
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> MyMarks()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var student = await _context.Students
                .Include(s => s.Department)
                .Include(s => s.Semester)
                .FirstOrDefaultAsync(s => s.ApplicationUserId == user.Id || s.Email == user.Email);

            if (student == null)
            {
                TempData["ErrorMessage"] = "Student profile not found.";
                return RedirectToAction("Index", "Dashboard");
            }

            // Student can ONLY see published marks!
            var publishedMarks = await _context.InternalMarks
                .Include(m => m.Subject)
                .Include(m => m.Semester)
                .Include(m => m.Details)
                    .ThenInclude(d => d.AssessmentComponent)
                .Where(m => m.StudentId == student.Id && m.Status == MarkStatus.Published)
                .OrderBy(m => m.Subject!.SubjectCode)
                .ToListAsync();

            ViewBag.Student = student;
            return View(publishedMarks);
        }

        // GET: Marks/ViewSubjectMarks?subjectId=1&semesterId=5 (Read-only review)
        [Authorize(Roles = "Admin,Faculty")]
        public async Task<IActionResult> ViewSubjectMarks(int subjectId, int? semesterId)
        {
            var subject = await _context.Subjects
                .Include(s => s.Department)
                .Include(s => s.Semester)
                .Include(s => s.AssessmentComponents.OrderBy(c => c.DisplayOrder))
                .FirstOrDefaultAsync(s => s.Id == subjectId);

            if (subject == null) return NotFound();

            int semId = semesterId ?? subject.SemesterId;

            var marks = await _context.InternalMarks
                .Include(m => m.Student)
                .Include(m => m.Details)
                    .ThenInclude(d => d.AssessmentComponent)
                .Where(m => m.SubjectId == subjectId && m.SemesterId == semId)
                .OrderBy(m => m.Student!.EnrollmentNumber)
                .ToListAsync();

            ViewBag.Subject = subject;
            ViewBag.SemesterId = semId;

            return View(marks);
        }
    }
}
