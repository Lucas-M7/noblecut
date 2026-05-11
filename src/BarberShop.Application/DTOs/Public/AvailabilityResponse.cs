namespace BarberShop.Application.DTOs.Public;

public record AvailabilityResponse
{
    public string Date { get; init; } = string.Empty;
    public List<string> Slots { get; init; } = [];
}