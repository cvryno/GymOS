using GymOS.Domain.Enums;

namespace GymOS.Domain.Entities;

public class Invoice : BaseEntity
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public int MemberId { get; set; }
    public int? MembershipId { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; } = 0;
    public decimal Tax { get; set; } = 0;
    public decimal Total { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public DateTime DueDate { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? PaymentMethod { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public Member Member { get; set; } = null!;
    public Membership? Membership { get; set; }
}
