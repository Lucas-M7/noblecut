namespace BarberShop.Application.DTOs.Reports;

public class ReportSummaryResponse
{
    public PeriodSummary Today { get; set; } = new();
    public PeriodSummary SelectedPeriod { get; set; } = new();
    public string? MostPopularService { get; set; }
    public string? BestDayOfWeek { get; set; }
    public List<DailyRevenue> ChartData { get; set; } = [];
    public string PeriodLabel { get; set; } = string.Empty;
}

public class PeriodSummary
{
    public decimal Revenue { get; set; }
    public int Appointments { get; set; }
    public decimal? PreviousRevenue { get; set; }
    public int? PreviousAppointments { get; set; }

    // Variação percentual em relação ao período anterior
    public decimal? RevenueChangePercent =>
        PreviousRevenue is null or 0
            ? null
            : Math.Round((Revenue - PreviousRevenue.Value) / PreviousRevenue.Value * 100, 1);
}

public class DailyRevenue
{
    public string Date { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty; // "10/07" ou "Jul" dependendo do período
    public decimal Revenue { get; set; }
    public int Appointments { get; set; }
}