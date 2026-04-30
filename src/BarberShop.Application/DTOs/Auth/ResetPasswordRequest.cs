using System.ComponentModel.DataAnnotations;

namespace BarberShop.Application.DTOs.Auth;

public record ResetPasswordRequest
{
    [Required(ErrorMessage = "Token é obrigatório.")]
    [StringLength(200, ErrorMessage = "Token inválido.")]
    public string Token { get; init; } = string.Empty;

    [Required(ErrorMessage = "Nova senha é obrigatória.")]
    [StringLength(100, MinimumLength = 6,
        ErrorMessage = "Senha deve ter entre 6 e 100 caracteres.")]
    public string NewPassword { get; init; } = string.Empty;
}