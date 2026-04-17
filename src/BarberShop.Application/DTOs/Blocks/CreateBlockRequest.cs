using System.ComponentModel.DataAnnotations;

namespace BarberShop.Application.DTOs.Blocks;

public class CreateBlockRequest
{
    [Required(ErrorMessage = "Data de início é obrigatória.")]
    [RegularExpression(@"^\d{4}-\d{2}-\d{2}$", ErrorMessage = "Data deve estar no formato YYYY-MM-DD.")]
    public string StartDate { get; set; } = string.Empty;

    [Required(ErrorMessage = "Data de fim é obrigatória.")]
    [RegularExpression(@"^\d{4}-\d{2}-\d{2}$", ErrorMessage = "Data deve estar no formato YYYY-MM-DD.")]
    public string EndDate { get; set; } = string.Empty;

    [StringLength(255, ErrorMessage = "Motivo deve ter no máximo 255 caracteres.")]
    public string? Reason { get; set; }
}