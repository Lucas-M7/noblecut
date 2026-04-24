using BarberShop.Application.DTOs.Reports;
using BarberShop.Application.Helpers;
using BarberShop.Domain.Enums;
using BarberShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BarberShop.Application.Services;

public class ReportService(AppDbContext db)
{
    // Períodos disponíveis
    public static readonly string[] ValidPeriods =
        ["this-month", "last-month", "last-3-months", "this-year"];

    public async Task<ReportSummaryResponse> GetSummaryAsync(Guid userId, string period)
    {
        var profile = await db.BarberProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId)
            ?? throw new KeyNotFoundException("Perfil não encontrado.");

        var today = DateTimeHelper.TodayInBrasilia();

        // Calcula as datas de início e fim do período selecionado
        // e do período anterior (para comparação)
        var (periodStart, periodEnd, previousStart, previousEnd, periodLabel) =
            GetPeriodDates(period, today);

        // ── Busca os agendamentos do período atual
        var currentAppointments = await db.Appointments
            .Include(a => a.Service)
            .Where(a =>
                a.BarberProfileId == profile.Id &&
                a.Status == AppointmentStatus.Completed &&
                a.AppointmentDate >= periodStart &&
                a.AppointmentDate <= periodEnd)
            .Select(a => new
            {
                a.AppointmentDate,
                ServiceName = a.Service.Name,
                Price = a.Service.Price ?? 0
            })
            .ToListAsync();

        // ── Busca os agendamentos do período anterior (para comparação)
        var previousAppointments = await db.Appointments
            .Include(a => a.Service)
            .Where(a =>
                a.BarberProfileId == profile.Id &&
                a.Status == AppointmentStatus.Completed &&
                a.AppointmentDate >= previousStart &&
                a.AppointmentDate <= previousEnd)
            .Select(a => new
            {
                a.AppointmentDate,
                ServiceName = a.Service.Name,
                Price = a.Service.Price ?? 0
            })
            .ToListAsync();

        // ── Hoje 
        var todayData = await db.Appointments
            .Include(a => a.Service)
            .Where(a =>
                a.BarberProfileId == profile.Id &&
                a.Status == AppointmentStatus.Completed &&
                a.AppointmentDate == today)
            .Select(a => new { Price = a.Service.Price ?? 0 })
            .ToListAsync();

        // ── Serviço mais popular 
        var mostPopularService = currentAppointments
            .GroupBy(a => a.ServiceName)
            .OrderByDescending(g => g.Count())
            .Select(g => $"{g.Key} ({g.Count()}x)")
            .FirstOrDefault();

        // ── Melhor dia da semana 
        var dayNames = new[]
        {
            "Domingo", "Segunda", "Terça",
            "Quarta", "Quinta", "Sexta", "Sábado"
        };

        var bestDayOfWeek = currentAppointments
            .GroupBy(a => a.AppointmentDate.DayOfWeek)
            .OrderByDescending(g => g.Sum(a => a.Price))
            .Select(g => dayNames[(int)g.Key])
            .FirstOrDefault();

        // ── Dados do gráfico 
        var chartData = BuildChartData(
            period, periodStart, periodEnd, currentAppointments
                .Select(a => new AppointmentForChart(a.AppointmentDate, a.Price))
                .ToList());

        return new ReportSummaryResponse
        {
            PeriodLabel = periodLabel,
            Today = new PeriodSummary
            {
                Revenue = todayData.Sum(a => a.Price),
                Appointments = todayData.Count
            },
            SelectedPeriod = new PeriodSummary
            {
                Revenue = currentAppointments.Sum(a => a.Price),
                Appointments = currentAppointments.Count,
                PreviousRevenue = previousAppointments.Sum(a => a.Price),
                PreviousAppointments = previousAppointments.Count
            },
            MostPopularService = mostPopularService,
            BestDayOfWeek = bestDayOfWeek,
            ChartData = chartData
        };
    }

    // Calcula as datas de início/fim do período e do anterior
    private static (DateOnly start, DateOnly end,
                    DateOnly prevStart, DateOnly prevEnd,
                    string label)
        GetPeriodDates(string period, DateOnly today)
    {
        return period switch
        {
            "last-month" => (
                new DateOnly(today.Year, today.Month, 1).AddMonths(-1),
                new DateOnly(today.Year, today.Month, 1).AddDays(-1),
                new DateOnly(today.Year, today.Month, 1).AddMonths(-2),
                new DateOnly(today.Year, today.Month, 1).AddMonths(-1).AddDays(-1),
                $"{MonthName(today.AddMonths(-1))} {today.AddMonths(-1).Year}"
            ),
            "last-3-months" => (
                today.AddDays(-89),
                today,
                today.AddDays(-179),
                today.AddDays(-90),
                "Últimos 3 meses"
            ),
            "this-year" => (
                new DateOnly(today.Year, 1, 1),
                today,
                new DateOnly(today.Year - 1, 1, 1),
                new DateOnly(today.Year - 1, 12, 31),
                $"Ano {today.Year}"
            ),
            // this-month (padrão)
            _ => (
                new DateOnly(today.Year, today.Month, 1),
                today,
                new DateOnly(today.Year, today.Month, 1).AddMonths(-1),
                new DateOnly(today.Year, today.Month, 1).AddDays(-1),
                $"{MonthName(today)} {today.Year}"
            )
        };
    }

    // Gera os dados do gráfico de acordo com o período
    // Períodos curtos → agrupado por dia
    // Períodos longos → agrupado por mês
    private static List<DailyRevenue> BuildChartData(
        string period,
        DateOnly start,
        DateOnly end,
        List<AppointmentForChart> appointments)
    {
        // Últimos 3 meses e ano inteiro agrupam por mês
        if (period is "last-3-months" or "this-year")
        {
            var months = new List<DailyRevenue>();
            var current = new DateOnly(start.Year, start.Month, 1);
            var endMonth = new DateOnly(end.Year, end.Month, 1);

            while (current <= endMonth)
            {
                var monthData = appointments
                    .Where(a =>
                        a.Date.Year == current.Year &&
                        a.Date.Month == current.Month)
                    .ToList();

                months.Add(new DailyRevenue
                {
                    Date = current.ToString("yyyy-MM-dd"),
                    Label = $"{MonthName(current).Substring(0, 3)}/{current.Year % 100:D2}",
                    Revenue = monthData.Sum(a => a.Price),
                    Appointments = monthData.Count
                });

                current = current.AddMonths(1);
            }

            return months;
        }

        // Este mês e mês passado agrupam por dia
        var days = new List<DailyRevenue>();
        var day = start;

        while (day <= end)
        {
            var dayData = appointments
                .Where(a => a.Date == day)
                .ToList();

            days.Add(new DailyRevenue
            {
                Date = day.ToString("yyyy-MM-dd"),
                Label = day.Day.ToString(),
                Revenue = dayData.Sum(a => a.Price),
                Appointments = dayData.Count
            });

            day = day.AddDays(1);
        }

        return days;
    }

    private static string MonthName(DateOnly date)
    {
        string[] months = [
            "Janeiro", "Fevereiro", "Março", "Abril",
            "Maio", "Junho", "Julho", "Agosto",
            "Setembro", "Outubro", "Novembro", "Dezembro"
        ];
        return months[date.Month - 1];
    }

    // Record auxiliar para evitar tipos anônimos no método de gráfico
    private record AppointmentForChart(DateOnly Date, decimal Price);
}