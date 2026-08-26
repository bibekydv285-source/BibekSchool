using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using BibekSchool.Models;

namespace BibekSchool.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Student> Students { get; set; } = null!;
        public DbSet<Teacher> Teachers { get; set; } = null!;
        public DbSet<SchoolClass> SchoolClasses { get; set; } = null!;
        public DbSet<Subject> Subjects { get; set; } = null!;
        public DbSet<ClassSubject> ClassSubjects { get; set; } = null!;
        public DbSet<TeacherAssignment> TeacherAssignments { get; set; } = null!;
        public DbSet<Mark> Marks { get; set; } = null!;
        public DbSet<Result> Results { get; set; } = null!;
        public DbSet<Notification> Notifications { get; set; } = null!;
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; } = null!;
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ApplicationUser>(entity =>
            {
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.UserName).IsUnique();
            });

            builder.Entity<Student>(entity =>
            {
                entity.HasIndex(e => e.AdmissionNumber).IsUnique();
                entity.HasIndex(e => e.UserId).IsUnique();
                entity.HasOne(e => e.User)
                    .WithOne(u => u.Student)
                    .HasForeignKey<Student>(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Class)
                    .WithMany(c => c.Students)
                    .HasForeignKey(e => e.ClassId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<Teacher>(entity =>
            {
                entity.HasIndex(e => e.EmployeeId).IsUnique();
                entity.HasIndex(e => e.UserId).IsUnique();
                entity.Property(e => e.Salary).HasPrecision(18, 2);
                entity.HasOne(e => e.User)
                    .WithOne(u => u.Teacher)
                    .HasForeignKey<Teacher>(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<SchoolClass>(entity =>
            {
                entity.HasIndex(e => new { e.Name, e.Section, e.AcademicYear }).IsUnique();
                entity.HasOne(e => e.ClassTeacher)
                    .WithMany()
                    .HasForeignKey(e => e.ClassTeacherId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<Subject>(entity =>
            {
                entity.HasIndex(e => e.Code).IsUnique();
                entity.Property(e => e.FullMarks).HasPrecision(18, 2);
                entity.Property(e => e.PassMarks).HasPrecision(18, 2);
            });

            builder.Entity<ClassSubject>(entity =>
            {
                entity.HasIndex(e => new { e.ClassId, e.SubjectId }).IsUnique();
                entity.HasOne(e => e.Class)
                    .WithMany(c => c.ClassSubjects)
                    .HasForeignKey(e => e.ClassId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Subject)
                    .WithMany(s => s.ClassSubjects)
                    .HasForeignKey(e => e.SubjectId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<TeacherAssignment>(entity =>
            {
                entity.HasIndex(e => new { e.TeacherId, e.ClassId, e.SubjectId, e.AcademicYear }).IsUnique();
                entity.HasOne(e => e.Teacher)
                    .WithMany(t => t.Assignments)
                    .HasForeignKey(e => e.TeacherId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Class)
                    .WithMany(c => c.TeacherAssignments)
                    .HasForeignKey(e => e.ClassId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Subject)
                    .WithMany(s => s.TeacherAssignments)
                    .HasForeignKey(e => e.SubjectId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<Mark>(entity =>
            {
                entity.HasIndex(e => new { e.StudentId, e.SubjectId, e.ExamName, e.AcademicYear }).IsUnique();
                entity.Property(e => e.ObtainedMarks).HasPrecision(18, 2);
                entity.Property(e => e.FullMarks).HasPrecision(18, 2);
                entity.Property(e => e.PassMarks).HasPrecision(18, 2);
                entity.HasOne(e => e.Student)
                    .WithMany(s => s.Marks)
                    .HasForeignKey(e => e.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Subject)
                    .WithMany(s => s.Marks)
                    .HasForeignKey(e => e.SubjectId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Teacher)
                    .WithMany(t => t.Marks)
                    .HasForeignKey(e => e.TeacherId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Result>(entity =>
            {
                entity.HasIndex(e => new { e.StudentId, e.AcademicYear, e.Term }).IsUnique();
                entity.Property(e => e.TotalObtainedMarks).HasPrecision(18, 2);
                entity.Property(e => e.Percentage).HasPrecision(5, 2);
                entity.HasOne(e => e.Student)
                    .WithMany(s => s.Results)
                    .HasForeignKey(e => e.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<Notification>(entity =>
            {
                entity.HasIndex(e => e.TargetUserId);
                entity.HasIndex(e => e.TargetRole);
                entity.HasIndex(e => e.IsRead);
                entity.HasOne(e => e.TargetUser)
                    .WithMany()
                    .HasForeignKey(e => e.TargetUserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<PasswordResetToken>(entity =>
            {
                entity.Property(e => e.Token).HasMaxLength(450);
                entity.HasIndex(e => e.Token).IsUnique();
                entity.HasIndex(e => e.UserId);
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<AuditLog>(entity =>
            {
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => new { e.EntityType, e.EntityId });
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void UpdateTimestamps()
        {
            var now = DateTime.UtcNow;
            var entries = ChangeTracker.Entries()
                .Where(e => e.Entity is ITrackableTimestamps);

            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Added)
                {
                    ((ITrackableTimestamps)entry.Entity).CreatedAt = now;
                }
                else if (entry.State == EntityState.Modified)
                {
                    ((ITrackableTimestamps)entry.Entity).UpdatedAt = now;
                }
            }
        }
    }
}