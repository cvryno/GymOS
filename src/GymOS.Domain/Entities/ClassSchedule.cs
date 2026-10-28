namespace GymOS.Domain.Entities;

public class ClassSchedule : BaseEntity
{
    public int ClassId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string? Room { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public GymClass GymClass { get; set; } = null!;
    public ICollection<ClassEnrollment> Enrollments { get; set; } = new List<ClassEnrollment>();
}
