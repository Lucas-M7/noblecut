namespace BarberShop.Application.DTOs.WorkingHours;

public record UpdateWorkingHoursRequest
{
    public List<WorkingHourItem> Hours { get; init; } = [];
}

// WorkingHourItem também vira record
// A lista em si é mutável (necessário para o model binding do JSON)
// mas a referência à lista é imutável após a criação do request
public record WorkingHourItem
{
    public DayOfWeek DayOfWeek { get; init; }
    public bool IsOpen { get; init; }
    public string StartTime { get; init; } = "09:00";
    public string EndTime { get; init; } = "18:00";
    public bool HasLunchBreak { get; init; }
    public string? LunchStart { get; init; }
    public string? LunchEnd { get; init; }
}