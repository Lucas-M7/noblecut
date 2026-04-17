using System.ComponentModel.DataAnnotations;

namespace BarberShop.Application.DTOs.Auth;

public class ResetPasswordRequest
{
    [Required(ErrorMessage = "Token é obrigatório.")]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nova senha é obrigatória.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Senha deve ter entre 6 e 100 caracteres.")]
    public string NewPassword { get; set; } = string.Empty;
}