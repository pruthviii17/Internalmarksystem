using InternalMarksSystem.Models;
using InternalMarksSystem.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace InternalMarksSystem.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IMarksCalculationEngine calculationEngine)
        {
            // Apply migrations or ensure created
            await context.Database.EnsureCreatedAsync();

            // 1. Seed Roles
            string[] roles = { "Admin", "Faculty", "Student" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // 2. Seed Departments
            if (!await context.Departments.AnyAsync())
            {
                var departments = new List<Department>
                {
                    new() { Name = "Computer Science & Engineering", Code = "CSE", Description = "Department of Computer Science & Engineering" },
                    new() { Name = "Information Technology", Code = "IT", Description = "Department of Information Technology" },
                    new() { Name = "Electronics & Communication", Code = "ECE", Description = "Department of Electronics & Communication" }
                };
                await context.Departments.AddRangeAsync(departments);
                await context.SaveChangesAsync();
            }

            var cseDept = await context.Departments.FirstAsync(d => d.Code == "CSE");
            var itDept = await context.Departments.FirstAsync(d => d.Code == "IT");

            // 3. Seed Semesters
            if (!await context.Semesters.AnyAsync())
            {
                var semesters = new List<Semester>();
                for (int i = 1; i <= 8; i++)
                {
                    semesters.Add(new Semester
                    {
                        SemesterNumber = i,
                        Name = $"Semester {i}",
                        AcademicYear = "2024-2025",
                        IsActive = (i == 5) // Sem 5 is active
                    });
                }
                await context.Semesters.AddRangeAsync(semesters);
                await context.SaveChangesAsync();
            }

            var sem5 = await context.Semesters.FirstAsync(s => s.SemesterNumber == 5);

            // 4. Seed Subjects
            if (!await context.Subjects.AnyAsync())
            {
                var subjects = new List<Subject>
                {
                    new()
                    {
                        SubjectCode = "CS501",
                        SubjectName = "Database Management Systems",
                        DepartmentId = cseDept.Id,
                        SemesterId = sem5.Id,
                        Credits = 4,
                        MaxInternalMarks = 50m,
                        PassingMarks = 20m
                    },
                    new()
                    {
                        SubjectCode = "CS502",
                        SubjectName = "Operating Systems",
                        DepartmentId = cseDept.Id,
                        SemesterId = sem5.Id,
                        Credits = 4,
                        MaxInternalMarks = 50m,
                        PassingMarks = 20m
                    },
                    new()
                    {
                        SubjectCode = "CS503",
                        SubjectName = "Web Technologies & ASP.NET",
                        DepartmentId = cseDept.Id,
                        SemesterId = sem5.Id,
                        Credits = 3,
                        MaxInternalMarks = 50m,
                        PassingMarks = 20m
                    },
                    new()
                    {
                        SubjectCode = "IT501",
                        SubjectName = "Software Engineering",
                        DepartmentId = itDept.Id,
                        SemesterId = sem5.Id,
                        Credits = 4,
                        MaxInternalMarks = 50m,
                        PassingMarks = 20m
                    }
                };

                await context.Subjects.AddRangeAsync(subjects);
                await context.SaveChangesAsync();
            }

            var cs501 = await context.Subjects.FirstAsync(s => s.SubjectCode == "CS501");
            var cs502 = await context.Subjects.FirstAsync(s => s.SubjectCode == "CS502");
            var cs503 = await context.Subjects.FirstAsync(s => s.SubjectCode == "CS503");
            var it501 = await context.Subjects.FirstAsync(s => s.SubjectCode == "IT501");

            // 5. Seed Assessment Components for subjects
            if (!await context.AssessmentComponents.AnyAsync())
            {
                var subjectsList = new[] { cs501, cs502, cs503, it501 };
                var components = new List<AssessmentComponent>();

                foreach (var s in subjectsList)
                {
                    components.Add(new AssessmentComponent { SubjectId = s.Id, Name = "Internal Test 1", MaxMarks = 30m, WeightPercentage = 30m, DisplayOrder = 1 });
                    components.Add(new AssessmentComponent { SubjectId = s.Id, Name = "Internal Test 2", MaxMarks = 30m, WeightPercentage = 30m, DisplayOrder = 2 });
                    components.Add(new AssessmentComponent { SubjectId = s.Id, Name = "Assignments", MaxMarks = 20m, WeightPercentage = 20m, DisplayOrder = 3 });
                    components.Add(new AssessmentComponent { SubjectId = s.Id, Name = "Practical / Viva", MaxMarks = 20m, WeightPercentage = 20m, DisplayOrder = 4 });
                }

                await context.AssessmentComponents.AddRangeAsync(components);
                await context.SaveChangesAsync();
            }

            // 6. Seed Users & Profiles
            // Admin User
            string adminEmail = "admin@college.edu";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "Dr. A. K. Sharma (HOD)",
                    UserRole = "Admin",
                    EmailConfirmed = true,
                    IsActive = true
                };
                await userManager.CreateAsync(adminUser, "Admin@123");
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }

            // Faculty User 1
            string fac1Email = "faculty.sharma@college.edu";
            var fac1User = await userManager.FindByEmailAsync(fac1Email);
            if (fac1User == null)
            {
                fac1User = new ApplicationUser
                {
                    UserName = fac1Email,
                    Email = fac1Email,
                    FullName = "Prof. Rajesh Sharma",
                    UserRole = "Faculty",
                    EmailConfirmed = true,
                    IsActive = true
                };
                await userManager.CreateAsync(fac1User, "Faculty@123");
                await userManager.AddToRoleAsync(fac1User, "Faculty");

                var facProfile1 = new FacultyProfile
                {
                    ApplicationUserId = fac1User.Id,
                    EmployeeCode = "FAC001",
                    FullName = "Prof. Rajesh Sharma",
                    Email = fac1Email,
                    DepartmentId = cseDept.Id,
                    Designation = "Assistant Professor"
                };
                await context.FacultyProfiles.AddAsync(facProfile1);
                await context.SaveChangesAsync();
            }

            // Faculty User 2
            string fac2Email = "faculty.patel@college.edu";
            var fac2User = await userManager.FindByEmailAsync(fac2Email);
            if (fac2User == null)
            {
                fac2User = new ApplicationUser
                {
                    UserName = fac2Email,
                    Email = fac2Email,
                    FullName = "Dr. Ananya Patel",
                    UserRole = "Faculty",
                    EmailConfirmed = true,
                    IsActive = true
                };
                await userManager.CreateAsync(fac2User, "Faculty@123");
                await userManager.AddToRoleAsync(fac2User, "Faculty");

                var facProfile2 = new FacultyProfile
                {
                    ApplicationUserId = fac2User.Id,
                    EmployeeCode = "FAC002",
                    FullName = "Dr. Ananya Patel",
                    Email = fac2Email,
                    DepartmentId = itDept.Id,
                    Designation = "Associate Professor"
                };
                await context.FacultyProfiles.AddAsync(facProfile2);
                await context.SaveChangesAsync();
            }

            var faculty1 = await context.FacultyProfiles.FirstAsync(f => f.EmployeeCode == "FAC001");
            var faculty2 = await context.FacultyProfiles.FirstAsync(f => f.EmployeeCode == "FAC002");

            // 7. Seed Faculty Assignments
            if (!await context.FacultySubjectAssignments.AnyAsync())
            {
                var assignments = new List<FacultySubjectAssignment>
                {
                    new() { FacultyProfileId = faculty1.Id, SubjectId = cs501.Id, SemesterId = sem5.Id, AcademicYear = "2024-2025" },
                    new() { FacultyProfileId = faculty1.Id, SubjectId = cs502.Id, SemesterId = sem5.Id, AcademicYear = "2024-2025" },
                    new() { FacultyProfileId = faculty2.Id, SubjectId = cs503.Id, SemesterId = sem5.Id, AcademicYear = "2024-2025" },
                    new() { FacultyProfileId = faculty2.Id, SubjectId = it501.Id, SemesterId = sem5.Id, AcademicYear = "2024-2025" }
                };
                await context.FacultySubjectAssignments.AddRangeAsync(assignments);
                await context.SaveChangesAsync();
            }

            // 8. Seed Students
            var studentSeeds = new[]
            {
                new { Name = "Rahul Verma", Email = "student.rahul@college.edu", Enroll = "2022CSE001", Dept = cseDept.Id, Sem = sem5.Id, Year = 2022 },
                new { Name = "Sneha Nair", Email = "student.sneha@college.edu", Enroll = "2022CSE002", Dept = cseDept.Id, Sem = sem5.Id, Year = 2022 },
                new { Name = "Vikram Singh", Email = "student.vikram@college.edu", Enroll = "2022CSE003", Dept = cseDept.Id, Sem = sem5.Id, Year = 2022 },
                new { Name = "Pooja Rao", Email = "student.pooja@college.edu", Enroll = "2022CSE004", Dept = cseDept.Id, Sem = sem5.Id, Year = 2022 }
            };

            foreach (var s in studentSeeds)
            {
                var user = await userManager.FindByEmailAsync(s.Email);
                if (user == null)
                {
                    user = new ApplicationUser
                    {
                        UserName = s.Email,
                        Email = s.Email,
                        FullName = s.Name,
                        UserRole = "Student",
                        EmailConfirmed = true,
                        IsActive = true
                    };
                    await userManager.CreateAsync(user, "Student@123");
                    await userManager.AddToRoleAsync(user, "Student");

                    var student = new Student
                    {
                        ApplicationUserId = user.Id,
                        EnrollmentNumber = s.Enroll,
                        FullName = s.Name,
                        Email = s.Email,
                        DepartmentId = s.Dept,
                        SemesterId = s.Sem,
                        EnrollmentYear = s.Year
                    };
                    await context.Students.AddAsync(student);
                    await context.SaveChangesAsync();
                }
            }

            // 9. Seed Internal Marks
            if (!await context.InternalMarks.AnyAsync())
            {
                var students = await context.Students.Where(s => s.SemesterId == sem5.Id).ToListAsync();
                var cs501Components = await context.AssessmentComponents.Where(c => c.SubjectId == cs501.Id).OrderBy(c => c.DisplayOrder).ToListAsync();
                var cs502Components = await context.AssessmentComponents.Where(c => c.SubjectId == cs502.Id).OrderBy(c => c.DisplayOrder).ToListAsync();
                var cs503Components = await context.AssessmentComponents.Where(c => c.SubjectId == cs503.Id).OrderBy(c => c.DisplayOrder).ToListAsync();

                // CS501: Published results (Tests: 30, 30, 20, 20)
                var rawScoresCS501 = new[]
                {
                    new[] { 28m, 27m, 19m, 19m }, // Rahul: 46.50
                    new[] { 25m, 24m, 18m, 18m }, // Sneha: 42.50
                    new[] { 20m, 19m, 14m, 15m }, // Vikram: 34.00
                    new[] { 11m, 10m, 8m, 9m }    // Pooja: 19.00 (Fail)
                };

                for (int i = 0; i < students.Count; i++)
                {
                    var student = students[i];
                    var scores = rawScoresCS501[i];
                    var scoreDict = new Dictionary<int, decimal>();
                    for (int j = 0; j < cs501Components.Count; j++)
                    {
                        scoreDict[cs501Components[j].Id] = scores[j];
                    }

                    var calc = calculationEngine.Calculate(cs501.MaxInternalMarks, cs501.PassingMarks, cs501Components, scoreDict);

                    var mark = new InternalMark
                    {
                        StudentId = student.Id,
                        SubjectId = cs501.Id,
                        SemesterId = sem5.Id,
                        AcademicYear = "2024-2025",
                        CalculatedTotal = calc.CalculatedTotal,
                        MaxTotal = calc.MaxTotal,
                        Percentage = calc.Percentage,
                        Grade = calc.Grade,
                        IsPass = calc.IsPass,
                        Status = MarkStatus.Published,
                        SubmittedByUserId = fac1User.Id,
                        SubmittedAtUtc = DateTime.UtcNow.AddDays(-5),
                        ApprovedByUserId = adminUser.Id,
                        ApprovedAtUtc = DateTime.UtcNow.AddDays(-3),
                        PublishedByUserId = adminUser.Id,
                        PublishedAtUtc = DateTime.UtcNow.AddDays(-2),
                        Remarks = "Final verified marks"
                    };

                    await context.InternalMarks.AddAsync(mark);
                    await context.SaveChangesAsync();

                    foreach (var breakdown in calc.Breakdowns)
                    {
                        await context.InternalMarkDetails.AddAsync(new InternalMarkDetail
                        {
                            InternalMarkId = mark.Id,
                            AssessmentComponentId = breakdown.ComponentId,
                            MarksObtained = breakdown.MarksObtained
                        });
                    }
                }

                // CS502: Submitted results (Waiting for Admin review)
                var rawScoresCS502 = new[]
                {
                    new[] { 26m, 25m, 17m, 18m },
                    new[] { 27m, 26m, 19m, 19m },
                    new[] { 22m, 21m, 16m, 16m },
                    new[] { 15m, 16m, 12m, 13m }
                };

                for (int i = 0; i < students.Count; i++)
                {
                    var student = students[i];
                    var scores = rawScoresCS502[i];
                    var scoreDict = new Dictionary<int, decimal>();
                    for (int j = 0; j < cs502Components.Count; j++)
                    {
                        scoreDict[cs502Components[j].Id] = scores[j];
                    }

                    var calc = calculationEngine.Calculate(cs502.MaxInternalMarks, cs502.PassingMarks, cs502Components, scoreDict);

                    var mark = new InternalMark
                    {
                        StudentId = student.Id,
                        SubjectId = cs502.Id,
                        SemesterId = sem5.Id,
                        AcademicYear = "2024-2025",
                        CalculatedTotal = calc.CalculatedTotal,
                        MaxTotal = calc.MaxTotal,
                        Percentage = calc.Percentage,
                        Grade = calc.Grade,
                        IsPass = calc.IsPass,
                        Status = MarkStatus.Submitted,
                        SubmittedByUserId = fac1User.Id,
                        SubmittedAtUtc = DateTime.UtcNow.AddHours(-12),
                        Remarks = "Submitted for HOD evaluation"
                    };

                    await context.InternalMarks.AddAsync(mark);
                    await context.SaveChangesAsync();

                    foreach (var breakdown in calc.Breakdowns)
                    {
                        await context.InternalMarkDetails.AddAsync(new InternalMarkDetail
                        {
                            InternalMarkId = mark.Id,
                            AssessmentComponentId = breakdown.ComponentId,
                            MarksObtained = breakdown.MarksObtained
                        });
                    }
                }

                // CS503: Draft results (For Faculty to edit & submit)
                var rawScoresCS503 = new[]
                {
                    new[] { 24m, 25m, 18m, 17m },
                    new[] { 26m, 27m, 18m, 18m },
                    new[] { 18m, 19m, 14m, 13m },
                    new[] { 20m, 21m, 15m, 14m }
                };

                for (int i = 0; i < students.Count; i++)
                {
                    var student = students[i];
                    var scores = rawScoresCS503[i];
                    var scoreDict = new Dictionary<int, decimal>();
                    for (int j = 0; j < cs503Components.Count; j++)
                    {
                        scoreDict[cs503Components[j].Id] = scores[j];
                    }

                    var calc = calculationEngine.Calculate(cs503.MaxInternalMarks, cs503.PassingMarks, cs503Components, scoreDict);

                    var mark = new InternalMark
                    {
                        StudentId = student.Id,
                        SubjectId = cs503.Id,
                        SemesterId = sem5.Id,
                        AcademicYear = "2024-2025",
                        CalculatedTotal = calc.CalculatedTotal,
                        MaxTotal = calc.MaxTotal,
                        Percentage = calc.Percentage,
                        Grade = calc.Grade,
                        IsPass = calc.IsPass,
                        Status = MarkStatus.Draft,
                        Remarks = "Faculty working draft"
                    };

                    await context.InternalMarks.AddAsync(mark);
                    await context.SaveChangesAsync();

                    foreach (var breakdown in calc.Breakdowns)
                    {
                        await context.InternalMarkDetails.AddAsync(new InternalMarkDetail
                        {
                            InternalMarkId = mark.Id,
                            AssessmentComponentId = breakdown.ComponentId,
                            MarksObtained = breakdown.MarksObtained
                        });
                    }
                }

                await context.SaveChangesAsync();
            }

            // 10. Seed Audit Logs
            if (!await context.AuditLogs.AnyAsync())
            {
                var auditLogs = new List<AuditLog>
                {
                    new()
                    {
                        UserName = "admin@college.edu",
                        UserRole = "Admin",
                        Action = "System Initialization",
                        EntityName = "System",
                        Details = "Database seeded with initial departments, semesters, subjects, and roles.",
                        TimestampUtc = DateTime.UtcNow.AddDays(-10)
                    },
                    new()
                    {
                        UserName = "faculty.sharma@college.edu",
                        UserRole = "Faculty",
                        Action = "Save Draft Marks",
                        EntityName = "Subject",
                        EntityId = cs501.Id.ToString(),
                        Details = "Draft marks saved for CS501 Database Management Systems.",
                        TimestampUtc = DateTime.UtcNow.AddDays(-6)
                    },
                    new()
                    {
                        UserName = "faculty.sharma@college.edu",
                        UserRole = "Faculty",
                        Action = "Submit Marks",
                        EntityName = "Subject",
                        EntityId = cs501.Id.ToString(),
                        Details = "Marks submitted for CS501 for HOD approval.",
                        TimestampUtc = DateTime.UtcNow.AddDays(-5)
                    },
                    new()
                    {
                        UserName = "admin@college.edu",
                        UserRole = "Admin",
                        Action = "Approve Marks",
                        EntityName = "Subject",
                        EntityId = cs501.Id.ToString(),
                        Details = "Approved marks for CS501 after verification.",
                        TimestampUtc = DateTime.UtcNow.AddDays(-3)
                    },
                    new()
                    {
                        UserName = "admin@college.edu",
                        UserRole = "Admin",
                        Action = "Publish Marks",
                        EntityName = "Subject",
                        EntityId = cs501.Id.ToString(),
                        Details = "Results published for CS501 Database Management Systems. Visible to students.",
                        TimestampUtc = DateTime.UtcNow.AddDays(-2)
                    }
                };
                await context.AuditLogs.AddRangeAsync(auditLogs);
                await context.SaveChangesAsync();
            }
        }
    }
}
