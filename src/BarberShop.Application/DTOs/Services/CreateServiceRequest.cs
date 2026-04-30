using System.ComponentModel.DataAnnotations;

namespace BarberShop.Application.DTOs.Services;

public record CreateServiceRequest
{
    [Required(ErrorMessage = "Nome do serviço é obrigatório.")]
    [StringLength(100, MinimumLength = 2,
        ErrorMessage = "Nome deve ter entre 2 e 100 caracteres.")]
    public string Name { get; init; } = string.Empty;

    [Range(10, 480,
        ErrorMessage = "Duração deve ser entre 10 e 480 minutos.")]
    public int DurationMinutes { get; init; }

    [Range(0, 99999.99,
        ErrorMessage = "Preço deve ser entre R$ 0 e R$ 99.999,99.")]
    public decimal? Price { get; init; }
}