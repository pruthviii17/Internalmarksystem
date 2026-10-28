using InternalMarksSystem.Data;
using InternalMarksSystem.Models;
using InternalMarksSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace InternalMarksSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class FacultyController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditService _auditService;

        public FacultyController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IAuditService auditService)
        {
            _context = context;
            _userManager = userManager;
            _auditService = auditService;
        }

        // GET: Faculty
        public async Task<IActionResult> Index()
        {
            var faculties = await _context.FacultyProfiles
                .Include(f => f.Department)
                .Include(f => f.AssignedSubjects)
                    .ThenInclude(fa => fa.Subject)
                .OrderBy(f => f.FullName)
                .ToListAsync();

            return View(faculties);
        }

        // GET: Faculty/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.DepartmentId = new SelectList(await _context.Departments.OrderBy(d => d.Name).ToListAsync(), "Id", "Name");
            return View(new FacultyProfile { Designation = "Assistant Professor" });
        }

        // POST: Faculty/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FacultyProfile faculty, string? password)
        {
            if (await _context.FacultyProfiles.AnyAsync(f => f.EmployeeCode.ToLower() == faculty.EmployeeCode.ToLower()))
            {
                ModelState.AddModelError("EmployeeCode", "A faculty member with this Employee Code already exists.");
            }

            if (await _context.FacultyProfiles.AnyAsync(f => f.Email.ToLower() == faculty.Email.ToLower()) ||
                await _userManager.FindByEmailAsync(faculty.Email) != null)
            {
                ModelState.AddModelError("Email", "An account or faculty profile with this Email already exists.");
            }

            if (ModelState.IsValid)
            {
                string pwd = string.IsNullOrWhiteSpace(password) ? "Faculty@123" : password;
                var identityUser = new ApplicationUser
                {
                    UserName = faculty.Email,
                    Email = faculty.Email,
                    FullName = faculty.FullName,
                    UserRole = "Faculty",
                    EmailConfirmed = true,
                    IsActive = true
                };

                var result = await _userManager.CreateAsync(identityUser, pwd);
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    ViewBag.DepartmentId = new SelectList(await _context.Departments.ToListAsync(), "Id", "Name", faculty.DepartmentId);
                    return View(faculty);
                }

                await _userManager.AddToRoleAsync(identityUser, "Faculty");

                faculty.ApplicationUserId = identityUser.Id;
                faculty.CreatedAtUtc = DateTime.UtcNow;

                _context.FacultyProfiles.Add(faculty);
                await _context.SaveChangesAsync();

                await _auditService.LogAsync("Create Faculty", "FacultyProfile", faculty.Id.ToString(),
                    $"Created faculty profile {faculty.FullName} ({faculty.EmployeeCode}).");

                TempData["SuccessMessage"] = $"Faculty '{faculty.FullName}' created successfully with login credentials!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.DepartmentId = new SelectList(await _context.Departments.ToListAsync(), "Id", "Name", faculty.DepartmentId);
            return View(faculty);
        }

        // GET: Faculty/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var faculty = await _context.FacultyProfiles.FindAsync(id);
            if (faculty == null) return NotFound();

            ViewBag.DepartmentId = new SelectList(await _context.Departments.ToListAsync(), "Id", "Name", faculty.DepartmentId);
            return View(faculty);
        }

        // POST: Faculty/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, FacultyProfile faculty)
        {
            if (id != faculty.Id) return NotFound();

            if (await _context.FacultyProfiles.AnyAsync(f => f.Id != id && f.EmployeeCode.ToLower() == faculty.EmployeeCode.ToLower()))
            {
                ModelState.AddModelError("EmployeeCode", "Another faculty profile already has this Employee Code.");
            }

            if (await _context.FacultyProfiles.AnyAsync(f => f.Id != id && f.Email.ToLower() == faculty.Email.ToLower()))
            {
                ModelState.AddModelError("Email", "Another faculty profile already has this Email.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existing = await _context.FacultyProfiles.FindAsync(id);
                    if (existing == null) return NotFound();

                    existing.FullName = faculty.FullName;
                    existing.EmployeeCode = faculty.EmployeeCode;
                    existing.Email = faculty.Email;
                    existing.DepartmentId = faculty.DepartmentId;
                    existing.Designation = faculty.Designation;

                    _context.Update(existing);
                    await _context.SaveChangesAsync();

                    if (!string.IsNullOrEmpty(existing.ApplicationUserId))
                    {
                        var user = await _userManager.FindByIdAsync(existing.ApplicationUserId);
                        if (user != null)
                        {
                            user.FullName = faculty.FullName;
                            user.Email = faculty.Email;
                            user.UserName = faculty.Email;
                            await _userManager.UpdateAsync(user);
                        }
                    }

                    await _auditService.LogAsync("Update Faculty", "FacultyProfile", id.ToString(), $"Updated faculty {faculty.FullName}");
                    TempData["SuccessMessage"] = "Faculty profile updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.FacultyProfiles.AnyAsync(e => e.Id == id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewBag.DepartmentId = new SelectList(await _context.Departments.ToListAsync(), "Id", "Name", faculty.DepartmentId);
            return View(faculty);
        }

        // GET: Faculty/AssignSubject/5
        public async Task<IActionResult> AssignSubject(int id)
        {
            var faculty = await _context.FacultyProfiles
                .Include(f => f.AssignedSubjects)
                    .ThenInclude(fa => fa.Subject)
                .Include(f => f.Department)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (faculty == null) return NotFound();

            // Load subjects in same or all departments
            var subjects = await _context.Subjects
                .Include(s => s.Semester)
                .OrderBy(s => s.SubjectCode)
                .ToListAsync();

            ViewBag.Subjects = new SelectList(subjects, "Id", "SubjectName");
            ViewBag.Semesters = new SelectList(await _context.Semesters.OrderBy(s => s.SemesterNumber).ToListAsync(), "Id", "Name");

            return View(faculty);
        }

        // POST: Faculty/AssignSubject
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignSubject(int facultyProfileId, int subjectId, int semesterId, string academicYear = "2024-2025")
        {
            var faculty = await _context.FacultyProfiles.FindAsync(facultyProfileId);
            var subject = await _context.Subjects.FindAsync(subjectId);

            if (faculty == null || subject == null)
            {
                TempData["ErrorMessage"] = "Faculty or Subject not found.";
                return RedirectToAction(nameof(Index));
            }

            // Check if already assigned
            bool exists = await _context.FacultySubjectAssignments.AnyAsync(fa =>
                fa.FacultyProfileId == facultyProfileId &&
                fa.SubjectId == subjectId &&
                fa.SemesterId == semesterId &&
                fa.AcademicYear == academicYear);

            if (exists)
            {
                TempData["ErrorMessage"] = $"Subject '{subject.SubjectCode}' is already assigned to {faculty.FullName} for this semester and year.";
                return RedirectToAction(nameof(AssignSubject), new { id = facultyProfileId });
            }

            var assignment = new FacultySubjectAssignment
            {
                FacultyProfileId = facultyProfileId,
                SubjectId = subjectId,
                SemesterId = semesterId,
                AcademicYear = academicYear,
                AssignedAtUtc = DateTime.UtcNow
            };

            _context.FacultySubjectAssignments.Add(assignment);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync("Assign Subject", "FacultySubjectAssignment", assignment.Id.ToString(),
                $"Assigned {subject.SubjectCode} ({subject.SubjectName}) to {faculty.FullName}.");

            TempData["SuccessMessage"] = $"Subject '{subject.SubjectCode}' assigned to {faculty.FullName} successfully.";
            return RedirectToAction(nameof(AssignSubject), new { id = facultyProfileId });
        }

        // POST: Faculty/RemoveAssignment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveAssignment(int assignmentId)
        {
            var assignment = await _context.FacultySubjectAssignments
                .Include(fa => fa.FacultyProfile)
                .Include(fa => fa.Subject)
                .FirstOrDefaultAsync(fa => fa.Id == assignmentId);

            if (assignment == null)
            {
                TempData["ErrorMessage"] = "Assignment not found.";
                return RedirectToAction(nameof(Index));
            }

            int facultyId = assignment.FacultyProfileId;
            string details = $"Removed assignment of {assignment.Subject?.SubjectCode} from {assignment.FacultyProfile?.FullName}";

            _context.FacultySubjectAssignments.Remove(assignment);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync("Remove Subject Assignment", "FacultySubjectAssignment", assignmentId.ToString(), details);
            TempData["SuccessMessage"] = "Subject assignment removed successfully.";
            return RedirectToAction(nameof(AssignSubject), new { id = facultyId });
        }
    }
}
