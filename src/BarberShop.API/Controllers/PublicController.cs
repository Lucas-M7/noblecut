using BarberShop.Application.DTOs.Appointments;
using BarberShop.Application.Services;
using BarberShop.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace BarberShop.API.Controllers;

[ApiController]
[Route("api/public")]
public class PublicController(
    AppDbContext db,
    AppointmentService appointmentService,
    AvailabilityService availabilityService) : ControllerBase
{
    [HttpGet("{slug}")]
    public async Task<IActionResult> GetBarber(string slug)
    {
        var profile = await db.BarberProfiles
            .FirstOrDefaultAsync(p => p.Slug == slug.ToLower());

        if (profile is null)
            return NotFound(new { error = "Barbeiro não encontrado." });

        return Ok(new
        {
            profile.Id,
            profile.DisplayName,
            profile.BusinessName,
            profile.Phone,
            profile.Slug
        });
    }

    [HttpGet("{slug}/services")]
    public async Task<IActionResult> GetServices(string slug)
    {
        var profile = await db.BarberProfiles
            .FirstOrDefaultAsync(p => p.Slug == slug.ToLower());

        if (profile is null)
            return NotFound(new { error = "Barbeiro não encontrado." });

        // Regra 4: serviço inativo não aparece para o cliente
        var services = await db.Services
            .Where(s => s.BarberProfileId == profile.Id && s.IsActive)
            .OrderBy(s => s.Name)
            .Select(s => new
            {
                s.Id,
                s.Name,
                s.DurationMinutes,
                s.Price
            })
            .ToListAsync();

        return Ok(services);
    }

    [HttpGet("{slug}/availability")]
    public async Task<IActionResult> GetAvailability(
        string slug,
        [FromQuery] Guid serviceId,
        [FromQuery] string date)
    {

        if (string.IsNullOrWhiteSpace(date) || !DateOnly.TryParse(date, out var parsedDate))
            return BadRequest(new { error = "Data inválida. Use o formato YYYY-MM-DD." });

        var profile = await db.BarberProfiles
            .FirstOrDefaultAsync(p => p.Slug == slug.ToLower());

        if (profile is null)
            return NotFound(new { error = "Barbeiro não encontrado." });

        var slots = await availabilityService.GetAvailableSlotsAsync(profile.Id, serviceId, parsedDate);
        return Ok(new { date, slots });
    }

    [HttpPost("{slug}/appointments")]
    [EnableRateLimiting("public-appointments")]
    public async Task<IActionResult> CreateAppointment(
        string slug,
        [FromBody] CreateAppointmentRequest request)
    {
        var profile = await db.BarberProfiles
            .FirstOrDefaultAsync(p => p.Slug == slug.ToLower());

        if (profile is null)
            return NotFound(new { error = "Barbeiro não encontrado." });

        var result = await appointmentService.CreatePublicAsync(profile.Id, request);
        return Created(string.Empty, result);
    }
}