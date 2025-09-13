using KMSI.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace KMSI.Data
{
    public class KMSIDbContext : DbContext
    {
        public KMSIDbContext(DbContextOptions<KMSIDbContext> options) : base(options)
        {
        }

        // User Management & Authentication
        public DbSet<User> Users { get; set; }
        public DbSet<UserLevel> UserLevels { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<Site> Sites { get; set; }

        // Teacher Management
        public DbSet<Teacher> Teachers { get; set; }
        public DbSet<TeacherSchedule> TeacherSchedules { get; set; }
        public DbSet<TeacherPayroll> TeacherPayrolls { get; set; }

        // Student Management
        public DbSet<Student> Students { get; set; }
        public DbSet<StudentBilling> StudentBillings { get; set; }
        public DbSet<StudentExamination> StudentExaminations { get; set; }
        public DbSet<StudentGradeHistory> StudentGradeHistories { get; set; }

        // Academic Management
        public DbSet<Grade> Grades { get; set; }
        public DbSet<GradeBook> GradeBooks { get; set; }
        public DbSet<Registration> Registrations { get; set; }
        public DbSet<ClassSchedule> ClassSchedules { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<Examination> Examinations { get; set; }
        public DbSet<Certificate> Certificates { get; set; }

        // Inventory & Book Management
        public DbSet<Book> Books { get; set; }
        public DbSet<BookPrice> BookPrices { get; set; }
        public DbSet<Inventory> Inventories { get; set; }
        public DbSet<StockMovement> StockMovements { get; set; }
        public DbSet<BookRequisition> BookRequisitions { get; set; }
        public DbSet<BookRequisitionDetail> BookRequisitionDetails { get; set; }

        // Billing & Finance
        public DbSet<BillingPeriod> BillingPeriods { get; set; }

        // System Management
        public DbSet<SystemSetting> SystemSettings { get; set; }
        public DbSet<SystemNotification> SystemNotifications { get; set; }
        public DbSet<EmailTemplate> EmailTemplates { get; set; }
        public DbSet<EmailLog> EmailLogs { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships and constraints
            ConfigureUserManagement(modelBuilder);
            ConfigureTeacherManagement(modelBuilder);
            ConfigureStudentManagement(modelBuilder);
            ConfigureAcademicManagement(modelBuilder);
            ConfigureInventoryManagement(modelBuilder);
            ConfigureSystemManagement(modelBuilder);
        }

        private void ConfigureUserManagement(ModelBuilder modelBuilder)
        {
            // Company self-referencing relationship
            modelBuilder.Entity<Company>()
                .HasOne(c => c.ParentCompany)
                .WithMany(c => c.SubCompanies)
                .HasForeignKey(c => c.ParentCompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            // User unique constraints
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // UserLevel unique constraint
            modelBuilder.Entity<UserLevel>()
            .HasIndex(ul => ul.LevelCode)
                .IsUnique();
        }

        private void ConfigureTeacherManagement(ModelBuilder modelBuilder)
        {
            // Teacher has unique UserId (one-to-one with User)
            modelBuilder.Entity<Teacher>()
            .HasIndex(t => t.UserId)
                .IsUnique();
        }

        private void ConfigureStudentManagement(ModelBuilder modelBuilder)
        {
            // Student unique constraint
            modelBuilder.Entity<Student>()
                .HasIndex(s => s.StudentCode)
                .IsUnique();

            // StudentBilling unique constraint
            modelBuilder.Entity<StudentBilling>()
            .HasIndex(sb => sb.BillingNumber)
            .IsUnique();
        }

        private void ConfigureAcademicManagement(ModelBuilder modelBuilder)
        {
            // Certificate unique constraint
            modelBuilder.Entity<Certificate>()
                .HasIndex(c => c.CertificateNumber)
                .IsUnique();

            // Registration unique constraint
            modelBuilder.Entity<Registration>()
                .HasIndex(r => r.RegistrationCode)
            .IsUnique();
        }

        private void ConfigureInventoryManagement(ModelBuilder modelBuilder)
        {
            // Inventory unique constraint (one record per BookId per SiteId)
            modelBuilder.Entity<Inventory>()
                .HasIndex(i => new { i.SiteId, i.BookId })
                .IsUnique();

            // BookRequisition unique constraint
            modelBuilder.Entity<BookRequisition>()
                .HasIndex(br => br.RequisitionNumber)
                .IsUnique();

            // StockMovement relationships for FromSite and ToSite
            modelBuilder.Entity<StockMovement>()
                .HasOne(sm => sm.FromSite)
                .WithMany()
                .HasForeignKey(sm => sm.FromSiteId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StockMovement>()
                .HasOne(sm => sm.ToSite)
                .WithMany()
                .HasForeignKey(sm => sm.ToSiteId)
            .OnDelete(DeleteBehavior.Restrict);
        }

        private void ConfigureSystemManagement(ModelBuilder modelBuilder)
        {
            // System Settings unique constraint per company
            modelBuilder.Entity<SystemSetting>()
                .HasIndex(ss => new { ss.CompanyId, ss.SettingKey })
                .IsUnique();

            // TeacherPayroll unique constraint
            modelBuilder.Entity<TeacherPayroll>()
                .HasIndex(tp => tp.PayrollNumber)
                .IsUnique();

            // BookRequisition ApprovedBy relationship
            modelBuilder.Entity<BookRequisition>()
                .HasOne(br => br.ApprovedByUser)
                .WithMany()
                .HasForeignKey(br => br.ApprovedBy)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
