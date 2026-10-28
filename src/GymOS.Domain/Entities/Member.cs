using GymOS.Domain.Enums;

namespace GymOS.Domain.Entities;

public class Member : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public DateTime DateOfBirth { get; set; }
    public string Address { get; set; } = string.Empty;
    public string EmergencyContactName { get; set; } = string.Empty;
    public string EmergencyContactPhone { get; set; } = string.Empty;
    public MemberStatus Status { get; set; } = MemberStatus.Active;
    public string? Notes { get; set; }

    // Navigation
    public ICollection<Membership> Memberships { get; set; } = new List<Membership>();
    public ICollection<AccessLog> AccessLogs { get; set; } = new List<AccessLog>();
    public ICollection<ClassEnrollment> ClassEnrollments { get; set; } = new List<ClassEnrollment>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public ICollection<WhatsAppNotificationLog> NotificationLogs { get; set; } = new List<WhatsAppNotificationLog>();

    public string FullName => $"{FirstName} {LastName}";
}
