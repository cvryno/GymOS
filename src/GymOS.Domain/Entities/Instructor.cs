namespace GymOS.Domain.Entities;

public class Instructor : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public string Specialties { get; set; } = string.Empty; // JSON array
    public string? Certifications { get; set; }
    public string? Bio { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime HireDate { get; set; }

    // Navigation
    public ICollection<GymClass> Classes { get; set; } = new List<GymClass>();

    public string FullName => $"{FirstName} {LastName}";
}
