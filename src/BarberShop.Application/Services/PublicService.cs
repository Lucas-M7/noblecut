using BarberShop.Application.DTOs.Appointments;
using BarberShop.Application.DTOs.Public;
using BarberShop.Application.Resolvers;
using BarberShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BarberShop.Application.Services;

/// <summary>
/// Responsabilidade: lógica de negócio dos endpoints públicos.
///
/// Separar evita misturar lógica autenticada com lógica pública
/// no mesmo service.
/// </summary>
public class PublicService(
    AppDbContext db,
    BarberProfileResolver profileResolver,
    AvailabilityService availabilityService,
    AppointmentService appointmentService)
{
    public async Task<PublicBarberResponse> GetBarberAsync(string slug)
    {
        var profile = await profileResolver.ResolveBySlugAsync(slug);

        return new PublicBarberResponse
        {
            Id = profile.Id,
            DisplayName = profile.DisplayName,
            BusinessName = profile.BusinessName,
            Phone = profile.Phone,
            Slug = profile.Slug,
            PhotoUrl = profile.PhotoUrl,
            PrimaryColor = profile.PrimaryColor
        };
    }

    public async Task<List<PublicServiceResponse>> GetServicesAsync(string slug)
    {
        var profile = await profileResolver.ResolveBySlugAsync(slug);

        // Regra 4: serviços inativos não aparecem para o cliente
        return await db.Services
            .Where(s => s.BarberProfileId == profile.Id && s.IsActive)
            .OrderBy(s => s.Name)
            .Select(s => new PublicServiceResponse
            {
                Id = s.Id,
                Name = s.Name,
                DurationMinutes = s.DurationMinutes,
                Price = s.Price
            }).ToListAsync();
    }

    public async Task<AvailabilityResponse> GetAvailabilityAsync(
        string slug,
        Guid serviceId,
        string date)
    {
        if (serviceId == Guid.Empty)
            throw new InvalidOperationException("serviceId é obrigatório.");

        if (string.IsNullOrWhiteSpace(date) || !DateOnly.TryParse(date, out var parsedDate))
            throw new InvalidOperationException("Data inválida. Use o formato YYYY-MM-DD.");

        var profile = await profileResolver.ResolveBySlugAsync(slug);

        var slots = await availabilityService.GetAvailableSlotsAsync(profile.Id, serviceId, parsedDate);

        return new AvailabilityResponse
        {
            Date = date,
            Slots = slots
        };
    }

    public async Task<AppointmentResponse> CreateAppointmentAsync(string slug, CreateAppointmentRequest request)
    {
        var profile = await profileResolver.ResolveBySlugAsync(slug);

        // Delega a criação para o AppointmentService
        return await appointmentService.CreatePublicAsync(profile.Id, request);
    }
}