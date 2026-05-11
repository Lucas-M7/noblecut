using BarberShop.Application.DTOs.Appointments;
using BarberShop.Application.Services;
using BarberShop.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace BarberShop.API.Controllers;

[ApiController]
[Route("api/public")]
public class PublicController(PublicService publicService) : ControllerBase
{
    [HttpGet("{slug}")]
    public async Task<IActionResult> GetBarber(string slug)
    {
        var result = await publicService.GetBarberAsync(slug);
        return Ok(result);
    }

    [HttpGet("{slug}/services")]
    public async Task<IActionResult> GetServices(string slug)
    {
        var result = await publicService.GetServicesAsync(slug);
        return Ok(result);
    }

    [HttpGet("{slug}/availability")]
    public async Task<IActionResult> GetAvailability(
        string slug,
        [FromQuery] Guid serviceId,
        [FromQuery] string date)
    {
        var result = await publicService.GetAvailabilityAsync(slug, serviceId, date);
        return Ok(result);
    }

    [HttpPost("{slug}/appointments")]
    [EnableRateLimiting("public-appointments")]
    public async Task<IActionResult> CreateAppointment(
        string slug,
        [FromBody] CreateAppointmentRequest request)
    {
        var result = await publicService.CreateAppointmentAsync(slug, request);
        return Created(string.Empty, result);
    }
}