using GymOS.Domain.Enums;

namespace GymOS.Domain.Entities;

public class WhatsAppNotificationLog : BaseEntity
{
    public int MemberId { get; set; }
    public int MembershipId { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string MessageBody { get; set; } = string.Empty;
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
    public string? ErrorMessage { get; set; }
    public string? WhatsAppMessageId { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Member Member { get; set; } = null!;
}
