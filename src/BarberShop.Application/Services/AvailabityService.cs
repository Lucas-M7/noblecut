using BarberShop.Application.Helpers;
using BarberShop.Domain.Enums;
using BarberShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BarberShop.Application.Services;

public class AvailabilityService(AppDbContext db)
{
    public async Task<List<string>> GetAvailableSlotsAsync(
        Guid barberProfileId,
        Guid serviceId,
        DateOnly date)
    {
        var today = DateTimeHelper.TodayInBrasilia();

        if (date < today)
            throw new InvalidOperationException("Não é possível consultar datas no passado.");

        var service = await db.Services.FirstOrDefaultAsync(s =>
            s.Id == serviceId &&
            s.BarberProfileId == barberProfileId &&
            s.IsActive)
            ?? throw new KeyNotFoundException("Serviço não encontrado ou inativo.");

        // Regra: bloqueios têm prioridade absoluta
        var isBlocked = await db.ScheduleBlocks.AnyAsync(b =>
            b.BarberProfileId == barberProfileId &&
            b.StartDate <= date &&
            b.EndDate >= date);

        if (isBlocked)
            return [];

        // Determina as configurações do dia:
        // 1º tenta horário especial, 2º usa horário semanal
        bool isOpen;
        TimeOnly startTime, endTime;
        bool hasLunchBreak;
        TimeOnly? lunchStart, lunchEnd;

        var specialHour = await db.SpecialHours.FirstOrDefaultAsync(s =>
            s.BarberProfileId == barberProfileId && s.Date == date);

        if (specialHour is not null)
        {
            isOpen = specialHour.IsOpen;
            startTime = specialHour.StartTime;
            endTime = specialHour.EndTime;
            hasLunchBreak = specialHour.HasLunchBreak;
            lunchStart = specialHour.LunchStart;
            lunchEnd = specialHour.LunchEnd;
        }
        else
        {
            var workingHour = await db.WorkingHours.FirstOrDefaultAsync(w =>
                w.BarberProfileId == barberProfileId &&
                w.DayOfWeek == date.DayOfWeek);

            if (workingHour is null || !workingHour.IsOpen)
                return [];

            isOpen = workingHour.IsOpen;
            startTime = workingHour.StartTime;
            endTime = workingHour.EndTime;
            hasLunchBreak = workingHour.HasLunchBreak;
            lunchStart = workingHour.LunchStart;
            lunchEnd = workingHour.LunchEnd;
        }

        if (!isOpen)
            return [];

        var existingAppointments = await db.Appointments
            .Where(a =>
                a.BarberProfileId == barberProfileId &&
                a.AppointmentDate == date &&
                a.Status != AppointmentStatus.Cancelled)
            .Select(a => new { a.StartTime, a.EndTime })
            .ToListAsync();

        var slots = new List<string>();
        var serviceDuration = TimeSpan.FromMinutes(service.DurationMinutes);
        var current = startTime;

        while (current.ToTimeSpan() + serviceDuration <= endTime.ToTimeSpan())
        {
            var slotStart = current;
            var slotEnd = current.Add(serviceDuration);

            // Verifica sobreposição com horário de almoço
            var overlapsLunch = hasLunchBreak &&
                lunchStart.HasValue &&
                lunchEnd.HasValue &&
                slotStart < lunchEnd.Value &&
                slotEnd > lunchStart.Value;

            // Verifica conflito com agendamentos existentes
            var hasConflict = existingAppointments.Any(a =>
                slotStart < a.EndTime && slotEnd > a.StartTime);

            // Verifica se já passou (para hoje)
            var isInPast = date == today &&
                current.ToTimeSpan() <= DateTimeHelper.NowInBrasilia().TimeOfDay;

            if (!overlapsLunch && !hasConflict && !isInPast)
                slots.Add(current.ToString("HH:mm"));

            current = current.Add(serviceDuration);
        }

        return slots;
    }
}