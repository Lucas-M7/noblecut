using BarberShop.Application.DTOs.Reports;
using BarberShop.Application.Helpers;
using BarberShop.Domain.Enums;
using BarberShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BarberShop.Application.Services;

public class ReportService(AppDbContext db)
{
    public async Task<ReportSummaryResponse> GetSummaryAsync(Guid userId)
    {
        var profile = await db.BarberProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId)
            ?? throw new KeyNotFoundException("Perfil não encontrado.");

        var today = DateTimeHelper.TodayInBrasilia();
        var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
        var startOfMonth = new DateOnly(today.Year, today.Month, 1);
        var thirtyDaysAgo = today.AddDays(-29);

        var appointments = await db.Appointments
            .Include(a => a.Service)
            .Where(a =>
                a.BarberProfileId == profile.Id &&
                a.Status == AppointmentStatus.Completed &&
                a.AppointmentDate >= thirtyDaysAgo)
            .Select(a => new
            {
               a.AppointmentDate,
               Price = a.Service.Price ?? 0 
            }).ToListAsync();

        var todayAppointments = appointments
            .Where(a => a.AppointmentDate == today).ToList();

        var weekAppointments = appointments
            .Where(a => a.AppointmentDate >= startOfWeek && a.AppointmentDate <= today)
            .ToList();

        var monthAppointments = await db.Appointments
            .Include(a => a.Service)
            .Where(a =>
                a.BarberProfileId == profile.Id &&
                a.Status == AppointmentStatus.Completed &&
                a.AppointmentDate >= startOfMonth &&
                a.AppointmentDate <= today)
            .Select(a => new
            {
                a.AppointmentDate,
                a.ServiceId,
                ServiceName = a.Service.Name,
                Price = a.Service.Price ?? 0
            }).ToListAsync();

        var mostPopularService = monthAppointments
            .GroupBy(a => a.ServiceName)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefault();

        var dayNames = new[]
        {
          "Domingo", "Segunda", "Terça",
          "Quarta", "Quinta", "Sexta", "Sábado"
        };

        var bestDayOfWeek = monthAppointments
            .GroupBy(a => a.AppointmentDate.DayOfWeek)
            .OrderByDescending(g => g.Sum(a => a.Price))
            .Select(g => dayNames[(int)g.Key])
            .FirstOrDefault();

        var last30Days = Enumerable
            .Range(0, 30)
            .Select(i => thirtyDaysAgo.AddDays(i))
            .Select(date =>
            {
                var dayData = appointments
                    .Where(a => a.AppointmentDate == date)
                    .ToList();

                return new DailyRevenue
                {
                    Date = date.ToString("yyyy-MM-dd"),
                    Revenue = dayData.Sum(a => a.Price),
                    Appointments = dayData.Count
                };
            }).ToList();

        return new ReportSummaryResponse
        {
            Today = new PeriodSummary
            {
                Revenue = todayAppointments.Sum(a => a.Price),
                Appointments = todayAppointments.Count
            },
            ThisWeek = new PeriodSummary
            {
                Revenue = weekAppointments.Sum(a => a.Price),
                Appointments = weekAppointments.Count
            },
            ThisMonth = new PeriodSummary
            {
                Revenue = monthAppointments.Sum(a => a.Price),
                Appointments = monthAppointments.Count
            },
            MostPopularService = mostPopularService,
            BestDayOfWeek = bestDayOfWeek,
            Last30Days = last30Days
        };
    }
}