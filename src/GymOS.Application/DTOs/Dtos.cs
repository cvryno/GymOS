namespace GymOS.Application.DTOs;

// ========== Member DTOs ==========
public record MemberDto(
    int Id, string FirstName, string LastName, string Email,
    string PhoneNumber, string? PhotoUrl, string Status,
    DateTime DateOfBirth, string Address,
    string EmergencyContactName, string EmergencyContactPhone,
    DateTime CreatedAt, ActiveMembershipDto? ActiveMembership);

public record ActiveMembershipDto(
    int Id, string PlanName, DateTime StartDate,
    DateTime EndDate, string Status, int DaysRemaining);

public record CreateMemberDto(
    string FirstName, string LastName, string Email,
    string PhoneNumber, DateTime DateOfBirth, string Address,
    string EmergencyContactName, string EmergencyContactPhone,
    string? Notes);

public record UpdateMemberDto(
    string FirstName, string LastName, string Email,
    string PhoneNumber, DateTime DateOfBirth, string Address,
    string EmergencyContactName, string EmergencyContactPhone,
    string Status, string? Notes);

// ========== Membership DTOs ==========
public record MembershipDto(
    int Id, int MemberId, string MemberName, string MemberEmail,
    string MemberPhone, int PlanId, string PlanName,
    DateTime StartDate, DateTime EndDate, string Status,
    decimal AmountPaid, int DaysRemaining, bool NotificationSent);

public record CreateMembershipDto(
    int MemberId, int PlanId, DateTime StartDate, decimal AmountPaid, string? Notes);

// ========== Plan DTOs ==========
public record MembershipPlanDto(
    int Id, string Name, string Description, decimal Price,
    int DurationDays, bool IsActive, bool IncludesClasses,
    int? MaxClassesPerMonth);

public record CreateMembershipPlanDto(
    string Name, string Description, decimal Price,
    int DurationDays, bool IncludesClasses, int? MaxClassesPerMonth);

// ========== Instructor DTOs ==========
public record InstructorDto(
    int Id, string FirstName, string LastName, string Email,
    string PhoneNumber, string? PhotoUrl, string Specialties,
    string? Bio, bool IsActive, DateTime HireDate,
    int ClassCount);

public record CreateInstructorDto(
    string FirstName, string LastName, string Email,
    string PhoneNumber, string Specialties, string? Bio,
    DateTime HireDate, string? Certifications);

// ========== Class DTOs ==========
public record GymClassDto(
    int Id, string Name, string Description, string Category,
    int InstructorId, string InstructorName,
    int DurationMinutes, int MaxCapacity,
    string? Color, bool IsActive, int EnrolledCount,
    List<ScheduleDto> Schedules);

public record ScheduleDto(
    int Id, string DayOfWeek, string StartTime, string EndTime, string? Room);

public record CreateGymClassDto(
    string Name, string Description, string Category,
    int InstructorId, int DurationMinutes, int MaxCapacity,
    string? Color, List<CreateScheduleDto>? Schedules);

public record CreateScheduleDto(
    DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime, string? Room);

// ========== Equipment DTOs ==========
public record EquipmentDto(
    int Id, string Name, string Brand, string Model,
    string SerialNumber, string Status, string Category,
    DateTime PurchaseDate, decimal PurchasePrice,
    DateTime? LastMaintenanceDate, DateTime? NextMaintenanceDate,
    string? Location, string? Notes);

public record CreateEquipmentDto(
    string Name, string Brand, string Model, string SerialNumber,
    string Category, DateTime PurchaseDate, decimal PurchasePrice,
    string? Location, string? Notes);

// ========== Access DTOs ==========
public record AccessLogDto(
    int Id, int MemberId, string MemberName, string MemberPhoto,
    DateTime CheckIn, DateTime? CheckOut, string? Duration);

public record CheckInDto(int MemberId);

// ========== Invoice DTOs ==========
public record InvoiceDto(
    int Id, string InvoiceNumber, int MemberId, string MemberName,
    decimal Subtotal, decimal Discount, decimal Tax, decimal Total,
    string Status, DateTime DueDate, DateTime? PaidAt, string? PaymentMethod);

public record CreateInvoiceDto(
    int MemberId, int? MembershipId,
    decimal Subtotal, decimal Discount, decimal Tax,
    DateTime DueDate, string? Notes);

// ========== Auth DTOs ==========
public record LoginDto(string Email, string Password);

public record GoogleLoginDto(string IdToken);

public record AuthResponseDto(
    string AccessToken, string RefreshToken,
    string Email, string FullName, string Role, string? PhotoUrl);

public record RegisterDto(
    string FirstName, string LastName, string Email,
    string Password, string Role);

// ========== Dashboard DTOs ==========
public record DashboardStatsDto(
    int TotalMembers, int ActiveMembers, int NewMembersThisMonth,
    decimal RevenueThisMonth, decimal RevenueLastMonth,
    int TotalClasses, int ClassesToday,
    int MembershipsExpiringIn3Days,
    int MembershipsExpiringIn7Days,
    int ActiveMembershipsCount,
    List<MonthlyRevenueDto> MonthlyRevenue,
    List<MembershipsByPlanDto> MembershipsByPlan,
    List<TopClassDto> TopClasses,
    List<MemberDto> RecentMembers,
    List<MembershipDto> ExpiringMemberships);

public record MonthlyRevenueDto(string Month, decimal Revenue);
public record MembershipsByPlanDto(string PlanName, int Count, string Color);
public record TopClassDto(string ClassName, int EnrollmentCount);

// ========== Pagination ==========
public record PagedResult<T>(List<T> Items, int TotalCount, int Page, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
};
