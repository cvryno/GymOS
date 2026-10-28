using GymOS.Domain.Enums;

namespace GymOS.Domain.Entities;

public class MembershipPlan : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int DurationDays { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Features { get; set; } // JSON array of features
    public bool IncludesClasses { get; set; } = true;
    public int? MaxClassesPerMonth { get; set; }

    // Navigation
    public ICollection<Membership> Memberships { get; set; } = new List<Membership>();
}
