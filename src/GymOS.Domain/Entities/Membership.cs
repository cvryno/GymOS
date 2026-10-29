using GymOS.Domain.Enums;

namespace GymOS.Domain.Entities;

public class Membership : BaseEntity
{
    public int MemberId { get; set; }
    public int PlanId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public MembershipStatus Status { get; set; } = MembershipStatus.Active;
    public decimal AmountPaid { get; set; }
    public bool NotificationSent { get; set; } = false;
    public DateTime? NotificationSentAt { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public Member Member { get; set; } = null!;
    public MembershipPlan Plan { get; set; } = null!;
    public Invoice? Invoice { get; set; }

    // EndDate is the exclusive UTC instant at the start of the day after the membership ends.
    public bool IsExpired => IsExpiredAt(DateTime.UtcNow);
    public int DaysRemaining => DaysRemainingAt(DateTime.UtcNow);

    public bool IsExpiredAt(DateTime utcNow) => EndDate <= utcNow;
    public int DaysRemainingAt(DateTime utcNow) => Math.Max(0, (int)Math.Ceiling((EndDate - utcNow).TotalDays));
}
