using System.ComponentModel.DataAnnotations;

namespace BarberShop.Application.DTOs.Profile;

public class UpdateProfileRequest
{
    [Required(ErrorMessage = "Nome de exibição é obrigatório.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Nome deve ter entre 2 e 100 caracteres.")]
    public string DisplayName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nome do negócio é obrigatório.")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Nome do negócio deve ter entre 2 e 150 caracteres.")]
    public string BusinessName { get; set; } = string.Empty;

    [StringLength(20, ErrorMessage = "Telefone deve ter no máximo 20 caracteres.")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Slug é obrigatório.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Slug deve ter entre 2 e 100 caracteres.")]
    [RegularExpression(@"^[a-z0-9\-]+$", ErrorMessage = "Slug deve conter apenas letras minúsculas, números e hífens.")]
    public string Slug { get; set; } = string.Empty;
}