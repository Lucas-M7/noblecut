using BarberShop.Application.DTOs.Profile;
using BarberShop.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BarberShop.API.Controllers;

[Authorize]
public class ProfileController(ProfileService profileService) : BaseController
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var result = await profileService.GetAsync(GetUserId());
        return Ok(result);
    }

    [HttpPut]
    public async Task<IActionResult> Upsert([FromBody] UpdateProfileRequest request)
    {
        var result = await profileService.UpsertAsync(GetUserId(), request);
        return Ok(result);
    }
}