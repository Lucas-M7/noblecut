namespace BarberShop.Domain.Entities;

public class BarberProfile
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string BusinessName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public string PrimaryColor { get; set; } = "#18181b";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User User { get; set; } = null!;
    public ICollection<Service> Services { get; set; } = [];
    public ICollection<WorkingHour> WorkingHours { get; set; } = [];
    public ICollection<ScheduleBlock> ScheduleBlocks { get; set; } = [];
    public ICollection<Appointment> Appointments { get; set; } = [];
    public ICollection<SpecialHour> SpecialHours { get; set; } = [];
}