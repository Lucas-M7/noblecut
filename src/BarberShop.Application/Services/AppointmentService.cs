using BarberShop.Application.DTOs.Appointments;
using BarberShop.Application.Helpers;
using BarberShop.Domain.Entities;
using BarberShop.Domain.Enums;
using BarberShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BarberShop.Application.Services;

public class AppointmentService(AppDbContext db, AvailabilityService availabilityService)
{
    public async Task<List<AppointmentResponse>> GetAsync(Guid userId, DateOnly? date = null)
    {
        // Se não tiver perfil ainda, retorna lista vazia em vez de lançar erro
        var profile = await db.BarberProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile is null)
            return [];

        var query = db.Appointments
            .Include(a => a.Service)
            .Where(a => a.BarberProfileId == profile.Id);

        if (date.HasValue)
            query = query.Where(a => a.AppointmentDate == date.Value);

        return await query
            .OrderBy(a => a.AppointmentDate)
            .ThenBy(a => a.StartTime)
            .Select(a => ToResponse(a))
            .ToListAsync();
    }

    public async Task<AppointmentResponse> CreatePublicAsync(
        Guid barberProfileId,
        CreateAppointmentRequest request)
    {
        // Validar formato da data
        if (!DateOnly.TryParse(request.AppointmentDate, out var date))
            throw new InvalidOperationException("Data inválida. Use o formato YYYY-MM-DD");

        // Validar formato do horário
        if (!TimeOnly.TryParse(request.StartTime, out var startTime))
            throw new InvalidOperationException("Horário inválido. Use o formato HH:mm");

        // Regra 5: não permitir no passado
        var today = DateTimeHelper.TodayInBrasilia();
        if (date < today)
            throw new InvalidOperationException("Não é possível agendar em datas passadas.");

        // Verificar disponibilidades real (aplica todas as regras 1-9)
        var availableSlots = await availabilityService.GetAvailableSlotsAsync(
            barberProfileId, request.ServiceId, date);

        var slotStr = startTime.ToString("HH:mm");
        if (!availableSlots.Contains(slotStr))
            throw new InvalidOperationException("Horário não disponível.");

        var service = await db.Services.FindAsync(request.ServiceId)
            ?? throw new KeyNotFoundException("Serviço não encontrado.");

        var endTime = startTime.Add(TimeSpan.FromMinutes(service.DurationMinutes));

        var hasConflit = await db.Appointments.AnyAsync(a =>
        a.BarberProfileId == barberProfileId &&
        a.AppointmentDate == date &&
        a.Status != AppointmentStatus.Cancelled &&
        startTime < a.EndTime &&
        endTime > a.StartTime);

        if (hasConflit)
            throw new InvalidOperationException("Este horário acabou de ser reservado. Escolha outro.");

        var appointment = new Appointment
        {
            Id = Guid.NewGuid(),
            BarberProfileId = barberProfileId,
            ServiceId = request.ServiceId,
            ClientName = Sanitizer.Trim(request.ClientName),
            ClientPhone = Sanitizer.Phone(request.ClientPhone),
            AppointmentDate = date,
            StartTime = startTime,
            EndTime = endTime,
            Status = AppointmentStatus.Scheduled
        };

        db.Appointments.Add(appointment);
        await db.SaveChangesAsync();

        await db.Entry(appointment).Reference(a => a.Service).LoadAsync();
        return ToResponse(appointment);
    }

    // Regra 9: cancelar libera o horário automaticamente
    public async Task<AppointmentResponse> CancelAsync(Guid userId, Guid appointmentId)
    {
        var appointment = await GetOwnedAppointmentAsync(userId, appointmentId);

        if (appointment.Status == AppointmentStatus.Cancelled)
            throw new InvalidOperationException("Agendamento já está cancelado.");

        appointment.Status = AppointmentStatus.Cancelled;
        await db.SaveChangesAsync();

        return ToResponse(appointment);
    }

    public async Task<AppointmentResponse> CompleteAsync(Guid userId, Guid appointmentId)
    {
        var appointment = await GetOwnedAppointmentAsync(userId, appointmentId);

        if (appointment.Status == AppointmentStatus.Cancelled)
            throw new InvalidOperationException("Não é possível concluir um agendamento cancelado.");

        if (appointment.Status == AppointmentStatus.Completed)
            throw new InvalidOperationException("Agendamento já está concluído.");

        appointment.Status = AppointmentStatus.Completed;
        await db.SaveChangesAsync();

        return ToResponse(appointment);
    }

    private async Task<BarberProfile> GetProfileAsync(Guid userId)
    {
        return await db.BarberProfiles.FirstOrDefaultAsync(p => p.UserId == userId)
            ?? throw new KeyNotFoundException("Perfil não encontrado.");
    }

    private async Task<Appointment> GetOwnedAppointmentAsync(Guid userId, Guid appointmentId)
    {
        var profile = await GetProfileAsync(userId);

        return await db.Appointments
            .Include(a => a.Service)
            .FirstOrDefaultAsync(a =>
            a.Id == appointmentId &&
            a.BarberProfileId == profile.Id)
            ?? throw new KeyNotFoundException("Agendamento não encontrado.");
    }

    private static AppointmentResponse ToResponse(Appointment a) => new()
    {
        Id = a.Id,
        ClientName = a.ClientName,
        ClientPhone = a.ClientPhone,
        ServiceName = a.Service.Name,
        ServiceDuration = a.Service.DurationMinutes,
        ServicePrice = a.Service.Price,
        AppointmentDate = a.AppointmentDate.ToString("yyyy-MM-dd"),
        StartTime = a.StartTime.ToString("HH:mm"),
        EndTime = a.EndTime.ToString("HH:mm"),
        Status = a.Status.ToString()
    };
}