namespace GymOS.Domain.Entities;

public class GymClass : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int InstructorId { get; set; }
    public int DurationMinutes { get; set; }
    public int MaxCapacity { get; set; }
    public string? ImageUrl { get; set; }
    public string? Color { get; set; } // For calendar display
    public bool IsActive { get; set; } = true;
    public string Category { get; set; } = string.Empty; // Yoga, CrossFit, etc.

    // Navigation
    public Instructor Instructor { get; set; } = null!;
    public ICollection<ClassSchedule> Schedules { get; set; } = new List<ClassSchedule>();
    public ICollection<ClassEnrollment> Enrollments { get; set; } = new List<ClassEnrollment>();
}
