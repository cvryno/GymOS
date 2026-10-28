namespace GymOS.Domain.Enums;

public enum MemberStatus
{
    Active,
    Inactive,
    Suspended,
    Expired
}

public enum MembershipStatus
{
    Active,
    Expired,
    Cancelled,
    Pending
}

public enum PaymentStatus
{
    Pending,
    Paid,
    Overdue,
    Refunded
}

public enum EquipmentStatus
{
    Operational,
    UnderMaintenance,
    Damaged,
    Retired
}

public enum UserRole
{
    Admin,
    Receptionist,
    Instructor
}

public enum NotificationStatus
{
    Pending,
    Sent,
    Failed
}
