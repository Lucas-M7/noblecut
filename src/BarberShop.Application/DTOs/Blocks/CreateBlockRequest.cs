using System.ComponentModel.DataAnnotations;

namespace BarberShop.Application.DTOs.Blocks;

public record CreateBlockRequest
{
    [Required(ErrorMessage = "Data de início é obrigatória.")]
    [RegularExpression(@"^\d{4}-\d{2}-\d{2}$",
        ErrorMessage = "Data de início deve estar no formato YYYY-MM-DD.")]
    public string StartDate { get; init; } = string.Empty;

    [Required(ErrorMessage = "Data de fim é obrigatória.")]
    [RegularExpression(@"^\d{4}-\d{2}-\d{2}$",
        ErrorMessage = "Data de fim deve estar no formato YYYY-MM-DD.")]
    public string EndDate { get; init; } = string.Empty;

    [StringLength(255, ErrorMessage = "Motivo deve ter no máximo 255 caracteres.")]
    public string? Reason { get; init; }
}