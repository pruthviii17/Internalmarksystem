using InternalMarksSystem.Data;
using InternalMarksSystem.Models;
using InternalMarksSystem.Services;
using InternalMarksSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace InternalMarksSystem.Controllers
{
    [Authorize(Roles = "Admin,Faculty")]
    public class SubjectsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IMarksCalculationEngine _calculationEngine;
        private readonly IAuditService _auditService;

        public SubjectsController(
            ApplicationDbContext context,
            IMarksCalculationEngine calculationEngine,
            IAuditService auditService)
        {
            _context = context;
            _calculationEngine = calculationEngine;
            _auditService = auditService;
        }

        // GET: Subjects
        public async Task<IActionResult> Index(string? search, int? departmentId, int? semesterId)
        {
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentDepartmentId = departmentId;
            ViewBag.CurrentSemesterId = semesterId;

            ViewBag.Departments = new SelectList(await _context.Departments.OrderBy(d => d.Name).ToListAsync(), "Id", "Name", departmentId);
            ViewBag.Semesters = new SelectList(await _context.Semesters.OrderBy(s => s.SemesterNumber).ToListAsync(), "Id", "Name", semesterId);

            var query = _context.Subjects
                .Include(s => s.Department)
                .Include(s => s.Semester)
                .Include(s => s.AssessmentComponents)
                .Include(s => s.FacultyAssignments)
                    .ThenInclude(fa => fa.FacultyProfile)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(s => s.SubjectCode.Contains(search) || s.SubjectName.Contains(search));
            }

            if (departmentId.HasValue && departmentId.Value > 0)
            {
                query = query.Where(s => s.DepartmentId == departmentId.Value);
            }

            if (semesterId.HasValue && semesterId.Value > 0)
            {
                query = query.Where(s => s.SemesterId == semesterId.Value);
            }

            var subjects = await query.OrderBy(s => s.SubjectCode).ToListAsync();
            return View(subjects);
        }

        // GET: Subjects/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var subject = await _context.Subjects
                .Include(s => s.Department)
                .Include(s => s.Semester)
                .Include(s => s.AssessmentComponents.OrderBy(c => c.DisplayOrder))
                .Include(s => s.FacultyAssignments)
                    .ThenInclude(fa => fa.FacultyProfile)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (subject == null) return NotFound();

            return View(subject);
        }

        // GET: Subjects/Create
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            ViewBag.DepartmentId = new SelectList(await _context.Departments.OrderBy(d => d.Name).ToListAsync(), "Id", "Name");
            ViewBag.SemesterId = new SelectList(await _context.Semesters.OrderBy(s => s.SemesterNumber).ToListAsync(), "Id", "Name");
            return View(new Subject { Credits = 4, MaxInternalMarks = 50m, PassingMarks = 20m });
        }

        // POST: Subjects/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Subject subject)
        {
            if (await _context.Subjects.AnyAsync(s => s.SubjectCode.ToLower() == subject.SubjectCode.ToLower()))
            {
                ModelState.AddModelError("SubjectCode", "A subject with this Subject Code already exists.");
            }

            if (subject.PassingMarks > subject.MaxInternalMarks)
            {
                ModelState.AddModelError("PassingMarks", "Passing marks cannot be greater than Maximum Internal Marks.");
            }

            if (ModelState.IsValid)
            {
                subject.CreatedAtUtc = DateTime.UtcNow;
                _context.Subjects.Add(subject);
                await _context.SaveChangesAsync();

                // Add default assessment components
                var defaultComponents = new List<AssessmentComponent>
                {
                    new() { SubjectId = subject.Id, Name = "Internal Test 1", MaxMarks = 30m, WeightPercentage = 30m, DisplayOrder = 1 },
                    new() { SubjectId = subject.Id, Name = "Internal Test 2", MaxMarks = 30m, WeightPercentage = 30m, DisplayOrder = 2 },
                    new() { SubjectId = subject.Id, Name = "Assignments", MaxMarks = 20m, WeightPercentage = 20m, DisplayOrder = 3 },
                    new() { SubjectId = subject.Id, Name = "Practical / Viva", MaxMarks = 20m, WeightPercentage = 20m, DisplayOrder = 4 }
                };

                _context.AssessmentComponents.AddRange(defaultComponents);
                await _context.SaveChangesAsync();

                await _auditService.LogAsync("Create Subject", "Subject", subject.Id.ToString(), 
                    $"Created subject {subject.SubjectCode} - {subject.SubjectName} with default assessment components.");

                TempData["SuccessMessage"] = $"Subject '{subject.SubjectCode} - {subject.SubjectName}' created successfully!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.DepartmentId = new SelectList(await _context.Departments.ToListAsync(), "Id", "Name", subject.DepartmentId);
            ViewBag.SemesterId = new SelectList(await _context.Semesters.ToListAsync(), "Id", "Name", subject.SemesterId);
            return View(subject);
        }

        // GET: Subjects/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var subject = await _context.Subjects.FindAsync(id);
            if (subject == null) return NotFound();

            ViewBag.DepartmentId = new SelectList(await _context.Departments.ToListAsync(), "Id", "Name", subject.DepartmentId);
            ViewBag.SemesterId = new SelectList(await _context.Semesters.ToListAsync(), "Id", "Name", subject.SemesterId);
            return View(subject);
        }

        // POST: Subjects/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, Subject subject)
        {
            if (id != subject.Id) return NotFound();

            if (await _context.Subjects.AnyAsync(s => s.Id != id && s.SubjectCode.ToLower() == subject.SubjectCode.ToLower()))
            {
                ModelState.AddModelError("SubjectCode", "Another subject already has this Subject Code.");
            }

            if (subject.PassingMarks > subject.MaxInternalMarks)
            {
                ModelState.AddModelError("PassingMarks", "Passing marks cannot be greater than Maximum Internal Marks.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existing = await _context.Subjects.FindAsync(id);
                    if (existing == null) return NotFound();

                    existing.SubjectCode = subject.SubjectCode;
                    existing.SubjectName = subject.SubjectName;
                    existing.DepartmentId = subject.DepartmentId;
                    existing.SemesterId = subject.SemesterId;
                    existing.Credits = subject.Credits;
                    existing.MaxInternalMarks = subject.MaxInternalMarks;
                    existing.PassingMarks = subject.PassingMarks;

                    _context.Update(existing);
                    await _context.SaveChangesAsync();

                    await _auditService.LogAsync("Update Subject", "Subject", id.ToString(), $"Updated subject {subject.SubjectCode}");
                    TempData["SuccessMessage"] = "Subject updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Subjects.AnyAsync(e => e.Id == id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewBag.DepartmentId = new SelectList(await _context.Departments.ToListAsync(), "Id", "Name", subject.DepartmentId);
            ViewBag.SemesterId = new SelectList(await _context.Semesters.ToListAsync(), "Id", "Name", subject.SemesterId);
            return View(subject);
        }

        // GET: Subjects/ConfigureComponents/5
        [Authorize(Roles = "Admin,Faculty")]
        public async Task<IActionResult> ConfigureComponents(int id)
        {
            var subject = await _context.Subjects
                .Include(s => s.AssessmentComponents.OrderBy(c => c.DisplayOrder))
                .FirstOrDefaultAsync(s => s.Id == id);

            if (subject == null) return NotFound();

            var model = new SubjectComponentConfigViewModel
            {
                SubjectId = subject.Id,
                SubjectCode = subject.SubjectCode,
                SubjectName = subject.SubjectName,
                MaxInternalMarks = subject.MaxInternalMarks,
                PassingMarks = subject.PassingMarks,
                Components = subject.AssessmentComponents.Select(c => new AssessmentComponentInputModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    MaxMarks = c.MaxMarks,
                    WeightPercentage = c.WeightPercentage,
                    DisplayOrder = c.DisplayOrder
                }).ToList()
            };

            return View(model);
        }

        // POST: Subjects/ConfigureComponents/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Faculty")]
        public async Task<IActionResult> ConfigureComponents(SubjectComponentConfigViewModel model)
        {
            var subject = await _context.Subjects
                .Include(s => s.AssessmentComponents)
                .Include(s => s.InternalMarks)
                .FirstOrDefaultAsync(s => s.Id == model.SubjectId);

            if (subject == null) return NotFound();

            // Prevent modifying components if published marks exist
            if (subject.InternalMarks.Any(m => m.Status == MarkStatus.Published))
            {
                ModelState.AddModelError(string.Empty, "Cannot reconfigure components because published results exist for this subject.");
                return View(model);
            }

            var dummyComponents = model.Components.Select(c => new AssessmentComponent
            {
                Id = c.Id,
                SubjectId = model.SubjectId,
                Name = c.Name,
                MaxMarks = c.MaxMarks,
                WeightPercentage = c.WeightPercentage
            }).ToList();

            if (!_calculationEngine.ValidateComponentWeights(dummyComponents, out string errorMessage))
            {
                ModelState.AddModelError(string.Empty, errorMessage);
                return View(model);
            }

            // Sync components
            var existingComponents = subject.AssessmentComponents.ToList();
            _context.AssessmentComponents.RemoveRange(existingComponents);

            int order = 1;
            foreach (var c in model.Components)
            {
                if (!string.IsNullOrWhiteSpace(c.Name))
                {
                    _context.AssessmentComponents.Add(new AssessmentComponent
                    {
                        SubjectId = model.SubjectId,
                        Name = c.Name.Trim(),
                        MaxMarks = c.MaxMarks,
                        WeightPercentage = c.WeightPercentage,
                        DisplayOrder = order++
                    });
                }
            }

            await _context.SaveChangesAsync();
            await _auditService.LogAsync("Configure Components", "Subject", model.SubjectId.ToString(), 
                $"Updated assessment components and weights for {subject.SubjectCode}.");

            TempData["SuccessMessage"] = "Assessment components and weights saved successfully.";
            return RedirectToAction(nameof(Details), new { id = model.SubjectId });
        }

        // POST: Subjects/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var subject = await _context.Subjects
                .Include(s => s.InternalMarks)
                .Include(s => s.AssessmentComponents)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (subject == null)
            {
                TempData["ErrorMessage"] = "Subject not found.";
                return RedirectToAction(nameof(Index));
            }

            if (subject.InternalMarks.Any())
            {
                TempData["ErrorMessage"] = "Cannot delete subject with existing internal marks. Clear or archive marks first.";
                return RedirectToAction(nameof(Index));
            }

            string code = subject.SubjectCode;
            _context.Subjects.Remove(subject);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync("Delete Subject", "Subject", id.ToString(), $"Deleted subject {code}");
            TempData["SuccessMessage"] = $"Subject '{code}' deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
