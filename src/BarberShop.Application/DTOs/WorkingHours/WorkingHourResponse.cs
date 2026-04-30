namespace BarberShop.Application.DTOs.WorkingHours;

public record WorkingHourResponse
{
    public Guid Id { get; init; }
    public DayOfWeek DayOfWeek { get; init; }
    public bool IsOpen { get; init; }
    public string StartTime { get; init; } = string.Empty;
    public string EndTime { get; init; } = string.Empty;
    public bool HasLunchBreak { get; init; }
    public string? LunchStart { get; init; }
    public string? LunchEnd { get; init; }
}