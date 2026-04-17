using System.ComponentModel.DataAnnotations;

namespace BarberShop.Application.DTOs.Services;

public class CreateServiceRequest
{
    [Required(ErrorMessage = "Nome do serviço é obrigatório.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Nome deve ter entre 2 e 100 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [Range(5, 480, ErrorMessage = "Duração deve ser entre 5 e 480 minutos.")]
    public int DurationMinutes { get; set; }

    [Range(0, 10000, ErrorMessage = "Preço deve ser entre 0 e 10.000.")]
    public decimal? Price { get; set; }
}