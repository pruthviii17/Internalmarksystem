using InternalMarksSystem.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace InternalMarksSystem.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Department> Departments => Set<Department>();
        public DbSet<Semester> Semesters => Set<Semester>();
        public DbSet<Subject> Subjects => Set<Subject>();
        public DbSet<AssessmentComponent> AssessmentComponents => Set<AssessmentComponent>();
        public DbSet<Student> Students => Set<Student>();
        public DbSet<FacultyProfile> FacultyProfiles => Set<FacultyProfile>();
        public DbSet<FacultySubjectAssignment> FacultySubjectAssignments => Set<FacultySubjectAssignment>();
        public DbSet<InternalMark> InternalMarks => Set<InternalMark>();
        public DbSet<InternalMarkDetail> InternalMarkDetails => Set<InternalMarkDetail>();
        public DbSet<ResultPublicationRecord> ResultPublicationRecords => Set<ResultPublicationRecord>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Department unique code
            builder.Entity<Department>(entity =>
            {
                entity.HasIndex(d => d.Code).IsUnique();
            });

            // Semester number + academic year index
            builder.Entity<Semester>(entity =>
            {
                entity.HasIndex(s => new { s.SemesterNumber, s.AcademicYear });
            });

            // Subject unique code
            builder.Entity<Subject>(entity =>
            {
                entity.HasIndex(s => s.SubjectCode).IsUnique();

                entity.HasOne(s => s.Department)
                    .WithMany(d => d.Subjects)
                    .HasForeignKey(s => s.DepartmentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(s => s.Semester)
                    .WithMany(sem => sem.Subjects)
                    .HasForeignKey(s => s.SemesterId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // AssessmentComponent
            builder.Entity<AssessmentComponent>(entity =>
            {
                entity.HasOne(ac => ac.Subject)
                    .WithMany(s => s.AssessmentComponents)
                    .HasForeignKey(ac => ac.SubjectId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Student unique enrollment & email
            builder.Entity<Student>(entity =>
            {
                entity.HasIndex(s => s.EnrollmentNumber).IsUnique();
                entity.HasIndex(s => s.Email).IsUnique();

                entity.HasOne(s => s.Department)
                    .WithMany(d => d.Students)
                    .HasForeignKey(s => s.DepartmentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(s => s.Semester)
                    .WithMany(sem => sem.Students)
                    .HasForeignKey(s => s.SemesterId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(s => s.ApplicationUser)
                    .WithOne(u => u.StudentProfile)
                    .HasForeignKey<Student>(s => s.ApplicationUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // FacultyProfile unique employee code & email
            builder.Entity<FacultyProfile>(entity =>
            {
                entity.HasIndex(f => f.EmployeeCode).IsUnique();
                entity.HasIndex(f => f.Email).IsUnique();

                entity.HasOne(f => f.Department)
                    .WithMany(d => d.FacultyMembers)
                    .HasForeignKey(f => f.DepartmentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(f => f.ApplicationUser)
                    .WithOne(u => u.FacultyProfile)
                    .HasForeignKey<FacultyProfile>(f => f.ApplicationUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // FacultySubjectAssignment unique assignment
            builder.Entity<FacultySubjectAssignment>(entity =>
            {
                entity.HasIndex(fsa => new { fsa.FacultyProfileId, fsa.SubjectId, fsa.SemesterId, fsa.AcademicYear }).IsUnique();

                entity.HasOne(fsa => fsa.FacultyProfile)
                    .WithMany(f => f.AssignedSubjects)
                    .HasForeignKey(fsa => fsa.FacultyProfileId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(fsa => fsa.Subject)
                    .WithMany(s => s.FacultyAssignments)
                    .HasForeignKey(fsa => fsa.SubjectId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // InternalMark unique constraint per student, subject, semester, academic year
            builder.Entity<InternalMark>(entity =>
            {
                entity.HasIndex(im => new { im.StudentId, im.SubjectId, im.SemesterId, im.AcademicYear }).IsUnique();

                entity.HasOne(im => im.Student)
                    .WithMany(s => s.InternalMarks)
                    .HasForeignKey(im => im.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(im => im.Subject)
                    .WithMany(s => s.InternalMarks)
                    .HasForeignKey(im => im.SubjectId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(im => im.Semester)
                    .WithMany()
                    .HasForeignKey(im => im.SemesterId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // InternalMarkDetail unique constraint per mark record and component
            builder.Entity<InternalMarkDetail>(entity =>
            {
                entity.HasIndex(imd => new { imd.InternalMarkId, imd.AssessmentComponentId }).IsUnique();

                entity.HasOne(imd => imd.InternalMark)
                    .WithMany(im => im.Details)
                    .HasForeignKey(imd => imd.InternalMarkId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(imd => imd.AssessmentComponent)
                    .WithMany(ac => ac.MarkDetails)
                    .HasForeignKey(imd => imd.AssessmentComponentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ResultPublicationRecord
            builder.Entity<ResultPublicationRecord>(entity =>
            {
                entity.HasOne(r => r.Subject)
                    .WithMany()
                    .HasForeignKey(r => r.SubjectId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
