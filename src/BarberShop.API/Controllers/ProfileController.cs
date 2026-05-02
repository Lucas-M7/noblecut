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

    [HttpPost("photo")]
    [RequestSizeLimit(5 * 1024 * 1024)] // 5MB máximo
    public async Task<IActionResult> UploadPhoto(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "Nenhum arquivo enviado." });

        if (file.Length > 5 * 1024 * 1024)
            return BadRequest(new { error = "Arquivo muito grande. Máximo 5MB." });

        await using var stream = file.OpenReadStream();
        var result = await profileService.UpdatePhotoAsync(
            GetUserId(), stream, file.FileName);

        return Ok(result);
    }

    [HttpDelete("photo")]
    public async Task<IActionResult> RemovePhoto()
    {
        var result = await profileService.RemovePhotoAsync(GetUserId());
        return Ok(result);
    }
}