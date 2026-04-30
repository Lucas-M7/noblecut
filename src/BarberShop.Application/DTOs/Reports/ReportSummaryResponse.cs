namespace BarberShop.Application.DTOs.Reports;

public record ReportSummaryResponse
{
    public string PeriodLabel { get; init; } = string.Empty;
    public PeriodSummary Today { get; init; } = new();
    public PeriodSummary SelectedPeriod { get; init; } = new();
    public string? MostPopularService { get; init; }
    public string? BestDayOfWeek { get; init; }
    public List<DailyRevenue> ChartData { get; init; } = [];
}

public record PeriodSummary
{
    public decimal Revenue { get; init; }
    public int Appointments { get; init; }
    public decimal? PreviousRevenue { get; init; }
    public int? PreviousAppointments { get; init; }

    // Propriedade calculada — permitida em records
    public decimal? RevenueChangePercent =>
        PreviousRevenue is null or 0
            ? null
            : Math.Round(
                (Revenue - PreviousRevenue.Value) / PreviousRevenue.Value * 100, 1);
}

public record DailyRevenue
{
    public string Date { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public decimal Revenue { get; init; }
    public int Appointments { get; init; }
}