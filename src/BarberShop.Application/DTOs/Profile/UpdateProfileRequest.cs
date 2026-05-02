using System.ComponentModel.DataAnnotations;

namespace BarberShop.Application.DTOs.Profile;

public record UpdateProfileRequest
{
    [Required(ErrorMessage = "Nome de exibição é obrigatório.")]
    [StringLength(100, MinimumLength = 2,
        ErrorMessage = "Nome de exibição deve ter entre 2 e 100 caracteres.")]
    public string DisplayName { get; init; } = string.Empty;

    [Required(ErrorMessage = "Nome do negócio é obrigatório.")]
    [StringLength(150, MinimumLength = 2,
        ErrorMessage = "Nome do negócio deve ter entre 2 e 150 caracteres.")]
    public string BusinessName { get; init; } = string.Empty;

    [StringLength(20, ErrorMessage = "Telefone deve ter no máximo 20 caracteres.")]
    public string Phone { get; init; } = string.Empty;

    [Required(ErrorMessage = "Slug é obrigatório.")]
    [StringLength(100, MinimumLength = 2,
        ErrorMessage = "Slug deve ter entre 2 e 100 caracteres.")]
    [RegularExpression(@"^[a-z0-9\-]+$",
        ErrorMessage = "Slug deve conter apenas letras minúsculas, números e hífens.")]
    public string Slug { get; init; } = string.Empty;

    [RegularExpression(@"^#[0-9A-Fa-f]{6}$",
        ErrorMessage = "Cor deve ser um hexadecimal válido. Ex: #1a2b3c")]
    public string PrimaryColor { get; init; } = "#18181b";
}