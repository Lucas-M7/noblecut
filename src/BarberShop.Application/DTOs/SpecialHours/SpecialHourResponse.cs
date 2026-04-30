namespace BarberShop.Application.DTOs.SpecialHours;

public record SpecialHourResponse
{
    public Guid Id { get; init; }
    public string Date { get; init; } = string.Empty;
    public bool IsOpen { get; init; }
    public string StartTime { get; init; } = string.Empty;
    public string EndTime { get; init; } = string.Empty;
    public bool HasLunchBreak { get; init; }
    public string? LunchStart { get; init; }
    public string? LunchEnd { get; init; }
    public string? Reason { get; init; }
}