namespace BarberShop.Application.DTOs.Appointments;

public record AppointmentResponse
{
    public Guid Id { get; init; }
    public string ClientName { get; init; } = string.Empty;
    public string ClientPhone { get; init; } = string.Empty;
    public string ServiceName { get; init; } = string.Empty;
    public int ServiceDuration { get; init; }
    public decimal? ServicePrice { get; init; }
    public string AppointmentDate { get; init; } = string.Empty;
    public string StartTime { get; init; } = string.Empty;
    public string EndTime { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
}