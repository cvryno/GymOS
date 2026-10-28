namespace GymOS.Domain.Entities;

public class AccessLog : BaseEntity
{
    public int MemberId { get; set; }
    public DateTime CheckIn { get; set; } = DateTime.UtcNow;
    public DateTime? CheckOut { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public Member Member { get; set; } = null!;

    public TimeSpan? Duration => CheckOut.HasValue ? CheckOut.Value - CheckIn : null;
}
