namespace BarberShop.Application.DTOs.Reports;

public class ReportSummaryResponse
{
    public PeriodSummary Today { get; set; } = new();
    public PeriodSummary ThisWeek { get; set; } = new();
    public PeriodSummary ThisMonth { get; set; } = new();
    public string? MostPopularService { get; set; }
    public string? BestDayOfWeek { get; set; }
    public List<DailyRevenue> Last30Days { get; set; } = [];
}

public class PeriodSummary
{
    public decimal Revenue { get; set; }
    public int Appointments { get; set; }
}

public class DailyRevenue
{
    public string Date { get; set; } = string.Empty;   // "2026-07-10"
    public decimal Revenue { get; set; }
    public int Appointments { get; set; }
}