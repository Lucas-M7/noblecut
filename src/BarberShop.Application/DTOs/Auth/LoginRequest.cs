using System.ComponentModel.DataAnnotations;

namespace BarberShop.Application.DTOs.Auth;

public record LoginRequest
{
    [Required(ErrorMessage = "E-mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "Formato de e-mail inválido.")]
    [StringLength(150, ErrorMessage = "E-mail deve ter no máximo 150 caracteres.")]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "Senha é obrigatória.")]
    [StringLength(100, MinimumLength = 6,
        ErrorMessage = "Senha deve ter entre 6 e 100 caracteres.")]
    public string Password { get; init; } = string.Empty;
}