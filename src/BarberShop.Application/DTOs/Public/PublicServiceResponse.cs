namespace BarberShop.Application.DTOs.Public;

public record PublicServiceResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public int DurationMinutes { get; init; }
    public decimal? Price { get; init; }
}