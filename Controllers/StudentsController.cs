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
    [Authorize(Roles = "Admin,Faculty")]
    public class StudentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditService _auditService;

        public StudentsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IAuditService auditService)
        {
            _context = context;
            _userManager = userManager;
            _auditService = auditService;
        }

        // GET: Students
        public async Task<IActionResult> Index(string? search, int? departmentId, int? semesterId, int page = 1)
        {
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentDepartmentId = departmentId;
            ViewBag.CurrentSemesterId = semesterId;

            ViewBag.Departments = new SelectList(await _context.Departments.OrderBy(d => d.Name).ToListAsync(), "Id", "Name", departmentId);
            ViewBag.Semesters = new SelectList(await _context.Semesters.OrderBy(s => s.SemesterNumber).ToListAsync(), "Id", "Name", semesterId);

            var query = _context.Students
                .Include(s => s.Department)
                .Include(s => s.Semester)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(s => s.FullName.Contains(search) || s.EnrollmentNumber.Contains(search) || s.Email.Contains(search));
            }

            if (departmentId.HasValue && departmentId.Value > 0)
            {
                query = query.Where(s => s.DepartmentId == departmentId.Value);
            }

            if (semesterId.HasValue && semesterId.Value > 0)
            {
                query = query.Where(s => s.SemesterId == semesterId.Value);
            }

            int pageSize = 10;
            int totalItems = await query.CountAsync();
            var students = await query
                .OrderBy(s => s.EnrollmentNumber)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            ViewBag.TotalItems = totalItems;

            return View(students);
        }

        // GET: Students/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var student = await _context.Students
                .Include(s => s.Department)
                .Include(s => s.Semester)
                .Include(s => s.InternalMarks)
                    .ThenInclude(m => m.Subject)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (student == null) return NotFound();

            return View(student);
        }

        // GET: Students/Create
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            ViewBag.DepartmentId = new SelectList(await _context.Departments.OrderBy(d => d.Name).ToListAsync(), "Id", "Name");
            ViewBag.SemesterId = new SelectList(await _context.Semesters.OrderBy(s => s.SemesterNumber).ToListAsync(), "Id", "Name");
            return View(new Student { EnrollmentYear = DateTime.UtcNow.Year });
        }

        // POST: Students/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Student student, string? password)
        {
            // Check for duplicate enrollment number
            if (await _context.Students.AnyAsync(s => s.EnrollmentNumber.ToLower() == student.EnrollmentNumber.ToLower()))
            {
                ModelState.AddModelError("EnrollmentNumber", "A student with this Enrollment Number already exists.");
            }

            // Check for duplicate email
            if (await _context.Students.AnyAsync(s => s.Email.ToLower() == student.Email.ToLower()) ||
                await _userManager.FindByEmailAsync(student.Email) != null)
            {
                ModelState.AddModelError("Email", "A student or user with this Email address already exists.");
            }

            if (ModelState.IsValid)
            {
                // Create login Identity user for the student
                string pwd = string.IsNullOrWhiteSpace(password) ? "Student@123" : password;
                var identityUser = new ApplicationUser
                {
                    UserName = student.Email,
                    Email = student.Email,
                    FullName = student.FullName,
                    UserRole = "Student",
                    EmailConfirmed = true,
                    IsActive = true
                };

                var userResult = await _userManager.CreateAsync(identityUser, pwd);
                if (!userResult.Succeeded)
                {
                    foreach (var error in userResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    ViewBag.DepartmentId = new SelectList(await _context.Departments.ToListAsync(), "Id", "Name", student.DepartmentId);
                    ViewBag.SemesterId = new SelectList(await _context.Semesters.ToListAsync(), "Id", "Name", student.SemesterId);
                    return View(student);
                }

                await _userManager.AddToRoleAsync(identityUser, "Student");

                student.ApplicationUserId = identityUser.Id;
                student.CreatedAtUtc = DateTime.UtcNow;

                _context.Students.Add(student);
                await _context.SaveChangesAsync();

                await _auditService.LogAsync("Create Student", "Student", student.Id.ToString(), 
                    $"Created student {student.FullName} ({student.EnrollmentNumber}). Default password: {pwd}");

                TempData["SuccessMessage"] = $"Student '{student.FullName}' created successfully with login credentials!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.DepartmentId = new SelectList(await _context.Departments.ToListAsync(), "Id", "Name", student.DepartmentId);
            ViewBag.SemesterId = new SelectList(await _context.Semesters.ToListAsync(), "Id", "Name", student.SemesterId);
            return View(student);
        }

        // GET: Students/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var student = await _context.Students.FindAsync(id);
            if (student == null) return NotFound();

            ViewBag.DepartmentId = new SelectList(await _context.Departments.ToListAsync(), "Id", "Name", student.DepartmentId);
            ViewBag.SemesterId = new SelectList(await _context.Semesters.ToListAsync(), "Id", "Name", student.SemesterId);
            return View(student);
        }

        // POST: Students/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, Student student)
        {
            if (id != student.Id) return NotFound();

            // Duplicate checks excluding self
            if (await _context.Students.AnyAsync(s => s.Id != id && s.EnrollmentNumber.ToLower() == student.EnrollmentNumber.ToLower()))
            {
                ModelState.AddModelError("EnrollmentNumber", "Another student already has this Enrollment Number.");
            }

            if (await _context.Students.AnyAsync(s => s.Id != id && s.Email.ToLower() == student.Email.ToLower()))
            {
                ModelState.AddModelError("Email", "Another student already has this Email.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existing = await _context.Students.FindAsync(id);
                    if (existing == null) return NotFound();

                    existing.FullName = student.FullName;
                    existing.EnrollmentNumber = student.EnrollmentNumber;
                    existing.Email = student.Email;
                    existing.DepartmentId = student.DepartmentId;
                    existing.SemesterId = student.SemesterId;
                    existing.EnrollmentYear = student.EnrollmentYear;

                    _context.Update(existing);
                    await _context.SaveChangesAsync();

                    // Update corresponding ApplicationUser if present
                    if (!string.IsNullOrEmpty(existing.ApplicationUserId))
                    {
                        var user = await _userManager.FindByIdAsync(existing.ApplicationUserId);
                        if (user != null)
                        {
                            user.FullName = student.FullName;
                            user.Email = student.Email;
                            user.UserName = student.Email;
                            await _userManager.UpdateAsync(user);
                        }
                    }

                    await _auditService.LogAsync("Update Student", "Student", id.ToString(), $"Updated student {student.FullName}");
                    TempData["SuccessMessage"] = "Student details updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Students.AnyAsync(e => e.Id == id))
                        return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewBag.DepartmentId = new SelectList(await _context.Departments.ToListAsync(), "Id", "Name", student.DepartmentId);
            ViewBag.SemesterId = new SelectList(await _context.Semesters.ToListAsync(), "Id", "Name", student.SemesterId);
            return View(student);
        }

        // POST: Students/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var student = await _context.Students
                .Include(s => s.InternalMarks)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (student == null)
            {
                TempData["ErrorMessage"] = "Student not found.";
                return RedirectToAction(nameof(Index));
            }

            if (student.InternalMarks.Any(m => m.Status == MarkStatus.Published))
            {
                TempData["ErrorMessage"] = "Cannot delete student because published internal marks records exist. Archive or withdraw marks first.";
                return RedirectToAction(nameof(Index));
            }

            string studentName = student.FullName;
            string? userId = student.ApplicationUserId;

            _context.Students.Remove(student);
            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(userId))
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    await _userManager.DeleteAsync(user);
                }
            }

            await _auditService.LogAsync("Delete Student", "Student", id.ToString(), $"Deleted student {studentName}");
            TempData["SuccessMessage"] = $"Student '{studentName}' deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
