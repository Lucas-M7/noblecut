using System.ComponentModel.DataAnnotations;

namespace BarberShop.Application.DTOs.Auth;

public record GoogleAuthRequest
{
    [Required(ErrorMessage = "O token do Google é obrigatório.")]
    public string IdToken { get; init; } = string.Empty;
}