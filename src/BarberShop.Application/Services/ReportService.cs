using BarberShop.Application.DTOs.Reports;
using BarberShop.Application.Helpers;
using BarberShop.Application.Resolvers;
using BarberShop.Domain.Enums;
using BarberShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BarberShop.Application.Services;

public class ReportService(
    AppDbContext db,
    BarberProfileResolver profileResolver)
{
    public static readonly string[] ValidPeriods =
        ["this-month", "last-month", "last-3-months", "this-year"];

    public async Task<ReportSummaryResponse> GetSummaryAsync(Guid userId, string period)
    {
        var profile = await profileResolver.ResolveAsync(userId);
        var today = DateTimeHelper.TodayInBrasilia();

        // Calcula as datas de todos os períodos necessários
        var (periodStart, periodEnd, previousStart, previousEnd, periodLabel) =
            GetPeriodDates(period, today);

        // Uma única query cobre todos os períodos
        // O range vai do início do período anterior até hoje.
        // Todos os subconjuntos (hoje, período atual, anterior) são filtrados
        // em memória a partir desse conjunto único.
        var allAppointments = await db.Appointments
            .Where(a =>
                a.BarberProfileId == profile.Id &&
                a.Status == AppointmentStatus.Completed &&
                a.AppointmentDate >= previousStart &&
                a.AppointmentDate <= today)
            .Select(a => new AppointmentData(
                a.AppointmentDate,
                a.Service.Name,
                a.Service.Price ?? 0))
            .ToListAsync();

        // Filtros em memória, sem mais round trips ao banco

        // Hoje: subconjunto do range total
        var todayData = allAppointments
            .Where(a => a.Date == today)
            .ToList();

        // Período selecionado: subconjunto do range total
        var currentData = allAppointments
            .Where(a => a.Date >= periodStart && a.Date <= periodEnd)
            .ToList();

        // Período anterior: para comparação (ex: mês passado vs retrasado)
        var previousData = allAppointments
            .Where(a => a.Date >= previousStart && a.Date <= previousEnd)
            .ToList();

        // Cálculos a partir dos dados já filtrados
        var mostPopularService = CalculateMostPopularService(currentData);
        var bestDayOfWeek = CalculateBestDayOfWeek(currentData);
        var chartData = BuildChartData(period, periodStart, periodEnd, currentData);

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
                Revenue = currentData.Sum(a => a.Price),
                Appointments = currentData.Count,
                PreviousRevenue = previousData.Sum(a => a.Price),
                PreviousAppointments = previousData.Count
            },
            MostPopularService = mostPopularService,
            BestDayOfWeek = bestDayOfWeek,
            ChartData = chartData
        };
    }

    // Cálculos em memória

    /// <summary>
    /// Agrupa por nome de serviço e retorna o mais frequente.
    /// Separado em método próprio por clareza, cada cálculo tem sua função.
    /// </summary>
    private static string? CalculateMostPopularService(
        List<AppointmentData> appointments)
    {
        return appointments
            .GroupBy(a => a.ServiceName)
            .OrderByDescending(g => g.Count())
            .Select(g => $"{g.Key} ({g.Count()}x)")
            .FirstOrDefault();
    }

    /// <summary>
    /// Retorna o dia da semana com maior faturamento no período.
    /// Usa o faturamento (não a quantidade) porque é o que importa
    /// para o barbeiro entender quando trabalha melhor.
    /// </summary>
    private static string? CalculateBestDayOfWeek(
        List<AppointmentData> appointments)
    {
        string[] dayNames =
        [
            "Domingo", "Segunda", "Terça",
            "Quarta", "Quinta", "Sexta", "Sábado"
        ];

        return appointments
            .GroupBy(a => a.Date.DayOfWeek)
            .OrderByDescending(g => g.Sum(a => a.Price))
            .Select(g => dayNames[(int)g.Key])
            .FirstOrDefault();
    }

    /// <summary>
    /// Constrói os dados para o gráfico.
    /// Períodos curtos → agrupados por dia.
    /// Períodos longos → agrupados por mês (menos barras, mais legível).
    /// </summary>
    private static List<DailyRevenue> BuildChartData(
        string period,
        DateOnly start,
        DateOnly end,
        List<AppointmentData> appointments)
    {
        return period is "last-3-months" or "this-year"
            ? BuildMonthlyChart(start, end, appointments)
            : BuildDailyChart(start, end, appointments);
    }

    private static List<DailyRevenue> BuildDailyChart(
        DateOnly start,
        DateOnly end,
        List<AppointmentData> appointments)
    {
        // Gera todos os dias do período, mesmo os sem agendamento
        // Garante que o gráfico não tenha lacunas
        var result = new List<DailyRevenue>();
        var current = start;

        while (current <= end)
        {
            var dayData = appointments
                .Where(a => a.Date == current)
                .ToList();

            result.Add(new DailyRevenue
            {
                Date = current.ToString("yyyy-MM-dd"),
                Label = current.Day.ToString(),
                Revenue = dayData.Sum(a => a.Price),
                Appointments = dayData.Count
            });

            current = current.AddDays(1);
        }

        return result;
    }

    private static List<DailyRevenue> BuildMonthlyChart(
        DateOnly start,
        DateOnly end,
        List<AppointmentData> appointments)
    {
        var result = new List<DailyRevenue>();
        var current = new DateOnly(start.Year, start.Month, 1);
        var endMonth = new DateOnly(end.Year, end.Month, 1);

        while (current <= endMonth)
        {
            var monthData = appointments
                .Where(a => a.Date.Year == current.Year
                         && a.Date.Month == current.Month)
                .ToList();

            result.Add(new DailyRevenue
            {
                Date = current.ToString("yyyy-MM-dd"),
                Label = $"{MonthName(current)[..3]}/{current.Year % 100:D2}",
                Revenue = monthData.Sum(a => a.Price),
                Appointments = monthData.Count
            });

            current = current.AddMonths(1);
        }

        return result;
    }

    // Datas dos períodos

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
            _ => ( // this-month (padrão)
                new DateOnly(today.Year, today.Month, 1),
                today,
                new DateOnly(today.Year, today.Month, 1).AddMonths(-1),
                new DateOnly(today.Year, today.Month, 1).AddDays(-1),
                $"{MonthName(today)} {today.Year}"
            )
        };
    }

    private static string MonthName(DateOnly date)
    {
        string[] months =
        [
            "Janeiro", "Fevereiro", "Março", "Abril",
            "Maio", "Junho", "Julho", "Agosto",
            "Setembro", "Outubro", "Novembro", "Dezembro"
        ];
        return months[date.Month - 1];
    }

    // Tipo auxiliar

    /// <summary>
    /// Projeção: contém apenas os campos necessários para todos os cálculos.
    /// Não carregamos o Appointment inteiro — só Date, ServiceName e Price.
    ///
    /// Isso é o mesmo padrão do AppointmentSlot no AvailabilityService:
    /// buscar do banco só o que você vai usar.
    /// </summary>
    private record AppointmentData(
        DateOnly Date,
        string ServiceName,
        decimal Price);
}