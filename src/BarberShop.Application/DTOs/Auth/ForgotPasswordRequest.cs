using System.ComponentModel.DataAnnotations;

namespace BarberShop.Application.DTOs.Auth;

public record ForgotPasswordRequest
{
    [Required(ErrorMessage = "E-mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "Formato de e-mail inválido.")]
    [StringLength(150, ErrorMessage = "E-mail deve ter no máximo 150 caracteres.")]
    public string Email { get; init; } = string.Empty;
}