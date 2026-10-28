using GymOS.Domain.Enums;

namespace GymOS.Domain.Entities;

public class Equipment : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public EquipmentStatus Status { get; set; } = EquipmentStatus.Operational;
    public DateTime PurchaseDate { get; set; }
    public decimal PurchasePrice { get; set; }
    public DateTime? LastMaintenanceDate { get; set; }
    public DateTime? NextMaintenanceDate { get; set; }
    public string? Location { get; set; }
    public string? ImageUrl { get; set; }
    public string? Notes { get; set; }
    public string Category { get; set; } = string.Empty; // Cardio, Weights, etc.
}
