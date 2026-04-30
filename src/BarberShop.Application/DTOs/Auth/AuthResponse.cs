namespace BarberShop.Application.DTOs.Auth;

public record AuthResponse
{
    public string Token { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public bool IsEmailConfirmed { get; init; }
}