namespace BarberShop.Application.DTOs.Profile;

public record ProfileResponse
{
    public Guid Id { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string BusinessName { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
}