using GymOS.Domain.Entities;
using GymOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GymOS.Infrastructure.Data;

public class GymDbContext : DbContext
{
    public GymDbContext(DbContextOptions<GymDbContext> options) : base(options) { }

    public DbSet<Member> Members => Set<Member>();
    public DbSet<Membership> Memberships => Set<Membership>();
    public DbSet<MembershipPlan> MembershipPlans => Set<MembershipPlan>();
    public DbSet<Instructor> Instructors => Set<Instructor>();
    public DbSet<GymClass> GymClasses => Set<GymClass>();
    public DbSet<ClassSchedule> ClassSchedules => Set<ClassSchedule>();
    public DbSet<ClassEnrollment> ClassEnrollments => Set<ClassEnrollment>();
    public DbSet<Equipment> Equipment => Set<Equipment>();
    public DbSet<AccessLog> AccessLogs => Set<AccessLog>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<WhatsAppNotificationLog> WhatsAppNotificationLogs => Set<WhatsAppNotificationLog>();
    public DbSet<AppUser> AppUsers => Set<AppUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Global soft delete filter
        modelBuilder.Entity<Member>().HasQueryFilter(m => !m.IsDeleted);
        modelBuilder.Entity<Instructor>().HasQueryFilter(i => !i.IsDeleted);
        modelBuilder.Entity<GymClass>().HasQueryFilter(c => !c.IsDeleted);
        modelBuilder.Entity<Equipment>().HasQueryFilter(e => !e.IsDeleted);

        // Member
        modelBuilder.Entity<Member>(e =>
        {
            e.Property(m => m.Email).IsRequired().HasMaxLength(200);
            e.HasIndex(m => m.Email).IsUnique();
            e.Property(m => m.Status).HasConversion<string>();
        });

        // Membership
        modelBuilder.Entity<Membership>(e =>
        {
            e.Property(m => m.Status).HasConversion<string>();
            e.HasOne(m => m.Member).WithMany(m => m.Memberships).HasForeignKey(m => m.MemberId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(m => m.Plan).WithMany(p => p.Memberships).HasForeignKey(m => m.PlanId).OnDelete(DeleteBehavior.Restrict);
        });

        // GymClass
        modelBuilder.Entity<GymClass>(e =>
        {
            e.HasOne(c => c.Instructor).WithMany(i => i.Classes).HasForeignKey(c => c.InstructorId).OnDelete(DeleteBehavior.Restrict);
        });

        // ClassSchedule
        modelBuilder.Entity<ClassSchedule>(e =>
        {
            e.Property(s => s.DayOfWeek).HasConversion<string>();
        });

        // ClassEnrollment
        modelBuilder.Entity<ClassEnrollment>(e =>
        {
            e.HasIndex(x => new { x.MemberId, x.ClassId, x.IsActive });
            e.HasOne(ce => ce.Member).WithMany(m => m.ClassEnrollments).HasForeignKey(ce => ce.MemberId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(ce => ce.GymClass).WithMany(c => c.Enrollments).HasForeignKey(ce => ce.ClassId).OnDelete(DeleteBehavior.Restrict);
        });

        // Equipment
        modelBuilder.Entity<Equipment>(e =>
        {
            e.Property(eq => eq.Status).HasConversion<string>();
        });

        // Invoice
        modelBuilder.Entity<Invoice>(e =>
        {
            e.Property(i => i.Status).HasConversion<string>();
            e.HasOne(i => i.Member).WithMany(m => m.Invoices).HasForeignKey(i => i.MemberId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(i => i.Membership).WithOne(m => m.Invoice).HasForeignKey<Invoice>(i => i.MembershipId).OnDelete(DeleteBehavior.SetNull);
        });

        // AppUser
        modelBuilder.Entity<AppUser>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Role).HasConversion<string>();
        });

        // WhatsAppNotificationLog
        modelBuilder.Entity<WhatsAppNotificationLog>(e =>
        {
            e.Property(n => n.Status).HasConversion<string>();
            e.HasOne(n => n.Member).WithMany(m => m.NotificationLogs).HasForeignKey(n => n.MemberId).OnDelete(DeleteBehavior.Restrict);
        });

        // Seed data
        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        // Seed membership plans
        modelBuilder.Entity<MembershipPlan>().HasData(
            new MembershipPlan
            {
                Id = 1, Name = "Básico Mensual", Description = "Acceso al gimnasio 30 días",
                Price = 29.99m, DurationDays = 30, IsActive = true, IncludesClasses = false,
                Features = "[\"Acceso al área de pesas\",\"Acceso a cardio\",\"Vestuarios\"]",
                CreatedAt = new DateTime(2024, 1, 1)
            },
            new MembershipPlan
            {
                Id = 2, Name = "Premium Mensual", Description = "Acceso completo con clases",
                Price = 49.99m, DurationDays = 30, IsActive = true, IncludesClasses = true,
                Features = "[\"Acceso total\",\"Clases ilimitadas\",\"Asesoría nutricional\",\"Evaluación física\"]",
                CreatedAt = new DateTime(2024, 1, 1)
            },
            new MembershipPlan
            {
                Id = 3, Name = "Trimestral", Description = "3 meses al mejor precio",
                Price = 119.99m, DurationDays = 90, IsActive = true, IncludesClasses = true,
                Features = "[\"Todo lo de Premium\",\"Descuento 20%\",\"Toalla incluida\"]",
                CreatedAt = new DateTime(2024, 1, 1)
            },
            new MembershipPlan
            {
                Id = 4, Name = "Anual", Description = "El mejor valor del año",
                Price = 399.99m, DurationDays = 365, IsActive = true, IncludesClasses = true,
                Features = "[\"Todo lo de Premium\",\"Descuento 33%\",\"Locker personal\",\"Bebida de bienvenida\"]",
                CreatedAt = new DateTime(2024, 1, 1)
            }
        );

        // Seed admin user (password: Admin@1234)
        modelBuilder.Entity<AppUser>().HasData(
            new AppUser
            {
                Id = 1,
                Email = "admin@gymos.com",
                FirstName = "Admin",
                LastName = "GymOS",
                PasswordHash = "$2a$11$rQCxJfL5uXELqLLfZ9vKQeGXVpEe2KGE9PVl7.7gQgxvhUvDHT7Ry", // Admin@1234
                Role = UserRole.Admin,
                IsActive = true,
                CreatedAt = new DateTime(2024, 1, 1)
            }
        );
    }
}
