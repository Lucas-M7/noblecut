using BarberShop.Application.Helpers;
using BarberShop.Domain.Entities;
using BarberShop.Domain.Enums;
using BarberShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BarberShop.Application.Services;

public class AvailabilityService(AppDbContext db)
{
    /// <summary>
    /// Ponto de entrada público(método). Orquestra busca e cálculo.
    /// 
    /// Responsabilidade: coordenar as etapas, não executá-las.
    /// Cada etapa é delegada a um método com responsabilidade única.
    /// </summary>
    public async Task<List<string>> GetAvailableSlotsAsync(Guid barberProfileId, Guid serviceId, DateOnly date)
    {
        var today = DateTimeHelper.TodayInBrasilia();

        if (date < today)
            throw new InvalidOperationException(
                "Não é possível consultar datas no passado.");

        // Busca todos os dados necessários em uma única etapa organizada
        var data = await FetchAvailabilityDataAsync(barberProfileId, serviceId, date);

        // Serviço não encontrado ou inativo
        if (data.Service is null)
            throw new KeyNotFoundException("Serviço não encontrado ou inativo.");

        // Bloqueios têm prioridade absoluta, retorna imediatamente
        if (data.IsBlocked)
            return [];

        // Determina qual configuração de horário usar:
        // Horário especial sobrepõe o semanal quando existir
        var schedule = data.SpecialHour is not null
            ? BuildScheduleFromSpecialHour(data.SpecialHour)
            : BuildScheduleFromWorkingHour(data.WorkingHour);

        // Dia fechado, retorna imediatamente
        if (!schedule.IsOpen)
            return [];

        // Delega o cálculo para um método síncrono e testável
        return CalculateSlots(schedule, data.Service, data.ExistingAppointments, date, today);
    }

    // ─── Busca de dados
    private async Task<AvailabilityData> FetchAvailabilityDataAsync(
        Guid barberProfileId,
        Guid serviceId,
        DateOnly date)
    {
        // Queries independentes executadas em paralelo
        var serviceTask = await db.Services
            .FirstOrDefaultAsync(s =>
                s.Id == serviceId &&
                s.BarberProfileId == barberProfileId &&
                s.IsActive);

        var isBlockedTask = await db.ScheduleBlocks
            .AnyAsync(b =>
                b.BarberProfileId == barberProfileId &&
                b.StartDate <= date &&
                b.EndDate >= date);

        // Se há bloqueio, não precisa buscar agendamentos
        // Evita uma query desnecessária
        if (isBlockedTask)
        {
            return new AvailabilityData(
                Service: serviceTask,
                IsBlocked: true,
                SpecialHour: null,
                WorkingHour: null,
                ExistingAppointments: []);
        }

        var specialHourTask = await db.SpecialHours
            .FirstOrDefaultAsync(s =>
                s.BarberProfileId == barberProfileId &&
                s.Date == date);

        var workingHourTask = await db.WorkingHours
            .FirstOrDefaultAsync(w =>
                w.BarberProfileId == barberProfileId &&
                w.DayOfWeek == date.DayOfWeek);

        // Busca agendamentos do dia (cancelados não bloqueiam horário)
        var existingAppointments = await db.Appointments
            .Where(a =>
                a.BarberProfileId == barberProfileId &&
                a.AppointmentDate == date &&
                a.Status != AppointmentStatus.Cancelled)
            .Select(a => new AppointmentSlot(a.StartTime, a.EndTime))
            .ToListAsync();

        return new AvailabilityData(
            Service: serviceTask,
            IsBlocked: false,
            SpecialHour: specialHourTask,
            WorkingHour: workingHourTask,
            ExistingAppointments: existingAppointments);
    }

    // ─── Construção do schedule

    /// <summary>
    /// Converte um SpecialHour em um DaySchedule normalizado.
    /// </summary>
    private static DaySchedule BuildScheduleFromSpecialHour(SpecialHour specialHour) =>
        new(
            IsOpen: specialHour.IsOpen,
            StartTime: specialHour.StartTime,
            EndTime: specialHour.EndTime,
            HasLunchBreak: specialHour.HasLunchBreak,
            LunchStart: specialHour.LunchStart,
            LunchEnd: specialHour.LunchEnd);

    private static DaySchedule BuildScheduleFromWorkingHour(WorkingHour? workingHour)
    {
        // Dia sem configuração = fechado
        if (workingHour is null)
            return new DaySchedule(false, default, default, false, null, null);

        return new(
            IsOpen: workingHour.IsOpen,
            StartTime: workingHour.StartTime,
            EndTime: workingHour.EndTime,
            HasLunchBreak: workingHour.HasLunchBreak,
            LunchStart: workingHour.LunchStart,
            LunchEnd: workingHour.LunchEnd);
    }

    // ─── Cálculo de slots

    /// <summary>
    /// Calcula os horários disponíveis. Método síncrono, não vai ao banco.
    /// 
    /// Todos os dados já foram buscados. Aqui só há operações em memória.
    /// Métodos async sem await real são um antipadrão, adicionam overhead
    /// sem benefício.
    /// 
    /// Regras aplicadas:
    /// - Slot deve caber inteiro antes do fim do expediente
    /// - Slot não pode sobrepor horário de almoço
    /// - Slot não pode conflitar com agendamentos existentes
    /// - Slot não pode estar no passado (para hoje)
    /// </summary>
    private static List<string> CalculateSlots(
        DaySchedule schedule,
        Service service,
        List<AppointmentSlot> existingAppointments,
        DateOnly date,
        DateOnly today)
    {
        var slots = new List<string>();
        var serviceDuration = TimeSpan.FromMinutes(service.DurationMinutes);
        var current = schedule.StartTime;
        var expedienteEnd = schedule.EndTime.ToTimeSpan();

        while (current.ToTimeSpan() + serviceDuration <= expedienteEnd)
        {
            var slotEnd = current.Add(serviceDuration);

            if (!HasConflict(current, slotEnd, schedule, existingAppointments, date, today))
                slots.Add(current.ToString("HH:mm"));

            current = current.Add(serviceDuration);
        }

        return slots;
    }

    /// <summary>
    /// Verifica se um slot tem algum impedimento.
    /// 
    /// Separado do loop principal por clareza:
    /// o loop decide quando avançar, esse método decide se o slot é válido.
    /// Cada responsabilidade no seu lugar.
    /// </summary>
    private static bool HasConflict(
        TimeOnly slotStart,
        TimeOnly slotEnd,
        DaySchedule schedule,
        List<AppointmentSlot> existingAppointments,
        DateOnly date,
        DateOnly today)
    {
        // Slot no passado (para o dia atual)
        if (date == today &&
            slotStart.ToTimeSpan() <= DateTimeHelper.NowInBrasilia().TimeOfDay)
            return true;

        // Slot sobrepõe horário de almoço
        if (schedule.HasLunchBreak &&
            schedule.LunchStart.HasValue &&
            schedule.LunchEnd.HasValue &&
            slotStart < schedule.LunchEnd.Value &&
            slotEnd > schedule.LunchStart.Value)
            return true;

        // Slot conflita com agendamento existente
        if (existingAppointments.Any(a =>
            slotStart < a.EndTime && slotEnd > a.StartTime))
            return true;

        return false;
    }

    public async Task<bool> IsSlotAvailableAsync(
        Guid barberProfileId,
        Guid serviceId,
        DateOnly date,
        TimeOnly starTime)
    {
        var today = DateTimeHelper.TodayInBrasilia();

        // Slot no passado nunca fica disponível
        if (date < today) return false;
        if (date == today &&
            starTime.ToTimeSpan() <= DateTimeHelper.NowInBrasilia().TimeOfDay)
            return false;

        var data = await FetchAvailabilityDataAsync(barberProfileId, serviceId, date);

        if (data.Service is null) return false;
        if (data.IsBlocked) return false;

        var schedule = data.SpecialHour is not null
            ? BuildScheduleFromSpecialHour(data.SpecialHour)
            : BuildScheduleFromWorkingHour(data.WorkingHour);

        if (!schedule.IsOpen) return false;

        var service = data.Service;
        var serviceDuration = TimeSpan.FromMinutes(service.DurationMinutes);
        var slotEnd = starTime.Add(serviceDuration);

        if (slotEnd.ToTimeSpan() > schedule.EndTime.ToTimeSpan()) return false;

        return !HasConflict(
            starTime,
            slotEnd,
            schedule,
            data.ExistingAppointments,
            date,
            today);
    }

    // ─── Tipos auxiliares

    /// <summary>
    /// Agrupa todos os dados buscados do banco.
    /// Record porque é apenas um container imutável de dados —
    /// não tem comportamento, só carrega informação.
    /// </summary>
    private record AvailabilityData(
        Service? Service,
        bool IsBlocked,
        SpecialHour? SpecialHour,
        WorkingHour? WorkingHour,
        List<AppointmentSlot> ExistingAppointments);

    /// <summary>
    /// Representa a configuração de horário do dia,
    /// independente da origem (WorkingHour ou SpecialHour).
    /// 
    /// Isso é abstração: o CalculateSlots trabalha com DaySchedule,
    /// não se preocupa de onde os dados vieram.
    /// </summary>
    private record DaySchedule(
        bool IsOpen,
        TimeOnly StartTime,
        TimeOnly EndTime,
        bool HasLunchBreak,
        TimeOnly? LunchStart,
        TimeOnly? LunchEnd);

    /// <summary>
    /// Representa apenas o que precisamos de um agendamento existente
    /// para verificar conflito. Não carregamos o objeto inteiro.
    /// 
    /// Isso é projeção — buscar só o que você precisa, não tudo.
    /// </summary>
    private record AppointmentSlot(TimeOnly StartTime, TimeOnly EndTime);
}