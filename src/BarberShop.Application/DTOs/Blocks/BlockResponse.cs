namespace BarberShop.Application.DTOs.Blocks;

public record BlockResponse
{
    public Guid Id { get; init; }
    public string StartDate { get; init; } = string.Empty;
    public string EndDate { get; init; } = string.Empty;
    public string? Reason { get; init; }
}