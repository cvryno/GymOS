namespace GymOS.Domain.Entities;

public class ClassEnrollment : BaseEntity
{
    public int MemberId { get; set; }
    public int ClassId { get; set; }
    public int? ScheduleId { get; set; }
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    // Navigation
    public Member Member { get; set; } = null!;
    public GymClass GymClass { get; set; } = null!;
    public ClassSchedule? Schedule { get; set; }
}
