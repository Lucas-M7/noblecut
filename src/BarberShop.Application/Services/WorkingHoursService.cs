using BarberShop.Application.DTOs.WorkingHours;
using BarberShop.Application.Resolvers;
using BarberShop.Domain.Entities;
using BarberShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BarberShop.Application.Services;

public class WorkingHoursService(AppDbContext db,
    BarberProfileResolver profileResolver)
{
    public async Task<List<WorkingHourResponse>> GetAsync(Guid userId)
    {
        var profile = await db.BarberProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile is null) return [];

        return await db.WorkingHours
            .Where(w => w.BarberProfileId == profile.Id)
            .OrderBy(w => w.DayOfWeek)
            .Select(w => ToResponse(w))
            .ToListAsync();
    }

    public async Task<List<WorkingHourResponse>> UpsertAsync(Guid userId, UpdateWorkingHoursRequest request)
    {
        var profile = await profileResolver.ResolveAsync(userId);
        var existing = await db.WorkingHours
            .Where(w => w.BarberProfileId == profile.Id)
            .ToListAsync();

        foreach (var item in request.Hours)
        {
            if (!TimeOnly.TryParse(item.StartTime, out var start))
                throw new InvalidOperationException($"Horário de início inválido para {item.DayOfWeek}.");

            if (!TimeOnly.TryParse(item.EndTime, out var end))
                throw new InvalidOperationException($"Horário de fim inválido para {item.DayOfWeek}.");

            if (item.IsOpen && end <= start)
                throw new InvalidOperationException($"Horário de fim deve ser após o início para {item.DayOfWeek}.");

            TimeOnly? lunchStart = null;
            TimeOnly? lunchEnd = null;

            if (item.HasLunchBreak)
            {
                if (!TimeOnly.TryParse(item.LunchStart, out var ls))
                    throw new InvalidOperationException($"Horário de início do almoço inválido para {item.DayOfWeek}.");

                if (!TimeOnly.TryParse(item.LunchEnd, out var le))
                    throw new InvalidOperationException($"Horário de fim do almoço inválido para {item.DayOfWeek}.");

                if (le <= ls)
                    throw new InvalidOperationException($"Fim do almoço deve ser após o início para {item.DayOfWeek}.");

                lunchStart = ls;
                lunchEnd = le;
            }

            var record = existing.FirstOrDefault(w => w.DayOfWeek == item.DayOfWeek);
            if (record is null)
            {
                record = new WorkingHour
                {
                    Id = Guid.NewGuid(),
                    BarberProfileId = profile.Id,
                    DayOfWeek = item.DayOfWeek,
                    IsOpen = item.IsOpen,
                    StartTime = start,
                    EndTime = end,
                    HasLunchBreak = item.HasLunchBreak,
                    LunchStart = lunchStart,
                    LunchEnd = lunchEnd
                };
                db.WorkingHours.Add(record);
            }
            else
            {
                record.IsOpen = item.IsOpen;
                record.StartTime = start;
                record.EndTime = end;
                record.HasLunchBreak = item.HasLunchBreak;
                record.LunchStart = lunchStart;
                record.LunchEnd = lunchEnd;
            }
        }

        await db.SaveChangesAsync();
        return await GetAsync(userId);
    }

    /// <summary>
    /// Valida e converte todos os itens do request antes de qualquer mutação.
    /// 
    /// Esse método está separado pois nenhum dado foi modificado ainda.
    /// Isso evita estados parcialmente modificados, ou valida tudo ou rejeita tudo.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    private static List<ParsedWorkingHour> ValidateAndParseHours(
        List<WorkingHourItem> hours)
    {
        var result = new List<ParsedWorkingHour>();

        foreach (var item in hours)
        {
            if (!TimeOnly.TryParse(item.StartTime, out var start))
                throw new InvalidOperationException(
                    $"Horário de início inválido para: {item.DayOfWeek}.");

            if (!TimeOnly.TryParse(item.EndTime, out var end))
                throw new InvalidOperationException(
                    $"Horário de fim inválido para: {item.DayOfWeek}.");

            if (item.IsOpen && end <= start)
                throw new InvalidOperationException(
                    $"Horário de fim deve ser após o início para: {item.DayOfWeek}.");

            TimeOnly? lunchStart = null;
            TimeOnly? lunchEnd = null;

            if (item.HasLunchBreak)
            {
                if (!TimeOnly.TryParse(item.LunchStart, out var ls))
                    throw new InvalidOperationException(
                        $"Horário de início do almoço inválido para {item.DayOfWeek}.");

                if (!TimeOnly.TryParse(item.LunchEnd, out var le))
                    throw new InvalidOperationException(
                        $"Horário de fim do almoço inválido para {item.DayOfWeek}.");

                if (le <= ls)
                    throw new InvalidOperationException(
                        $"Fim do almoço deve ser após o início para {item.DayOfWeek}.");

                lunchStart = ls;
                lunchEnd = le;
            }

            result.Add(new ParsedWorkingHour(
                item.DayOfWeek, item.IsOpen,
                start, end,
                item.HasLunchBreak, lunchStart, lunchEnd));
        }

        return result;
    }

    private static WorkingHourResponse ToResponse(WorkingHour w) => new()
    {
        Id = w.Id,
        DayOfWeek = w.DayOfWeek,
        IsOpen = w.IsOpen,
        StartTime = w.StartTime.ToString("HH:mm"),
        EndTime = w.EndTime.ToString("HH:mm"),
        HasLunchBreak = w.HasLunchBreak,
        LunchStart = w.LunchStart?.ToString("HH:mm"),
        LunchEnd = w.LunchEnd?.ToString("HH:mm")
    };

    private record ParsedWorkingHour(
        DayOfWeek DayOfWeek,
        bool IsOpen,
        TimeOnly StartTime,
        TimeOnly EndTime,
        bool HasLunchBreak,
        TimeOnly? LunchStart,
        TimeOnly? LunchEnd);
}