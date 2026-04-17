using System.ComponentModel.DataAnnotations;

namespace BarberShop.Application.DTOs.Auth;

public class ForgotPasswordRequest
{
    [Required(ErrorMessage = "E-mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "E-mail inválido.")]
    [StringLength(150, ErrorMessage = "E-mail deve ter no máximo 150 caracteres.")]
    public string Email { get; set; } = string.Empty;
}