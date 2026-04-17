using BarberShop.Application.DTOs.Auth;
using BarberShop.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BarberShop.API.Controllers;

public class AuthController(AuthService authService) : BaseController
{
    [HttpPost("register")]
    [EnableRateLimiting("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await authService.RegisterAsync(request);
        return Created(string.Empty, result);
    }

    [HttpPost("login")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await authService.LoginAsync(request);
        return Ok(result);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var result = await authService.GetMeAsync(GetUserId());
        return Ok(result);
    }

    // [HttpPost("confirm-email")]
    // public async Task<IActionResult> ConfirmEmail([FromQuery] string token)
    // {
    //     if (string.IsNullOrWhiteSpace(token))
    //         return BadRequest(new { error = "Token inválido." });

    //     await authService.ConfirmEmailAsync(token);
    //     return Ok(new { message = "E-mail confirmado com sucesso!" });
    // }

    [HttpPost("forgot-password")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        await authService.ForgotPasswordAsync(request);

        // Sempre retorna sucesso para não revelar se o e-mail existe
        return Ok(new { message = "Se este e-mail estiver cadastrado, você receberá as instruções em breve." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        await authService.ResetPasswordAsync(request);
        return Ok(new { message = "Senha redefinida com sucesso!" });
    }

    [Authorize]
    [HttpPost("resend-confirmation")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> ResendConfirmation()
    {
        await authService.ResendConfirmationAsync(GetUserId());
        return Ok(new { message = "E-mail de confirmação reenviado." });
    }
}