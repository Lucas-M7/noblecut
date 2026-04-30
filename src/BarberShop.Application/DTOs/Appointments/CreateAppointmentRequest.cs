using System.ComponentModel.DataAnnotations;

namespace BarberShop.Application.DTOs.Appointments;

public record CreateAppointmentRequest
{
    [Required(ErrorMessage = "Serviço é obrigatório.")]
    public Guid ServiceId { get; init; }

    [Required(ErrorMessage = "Nome do cliente é obrigatório.")]
    [StringLength(100, MinimumLength = 2,
        ErrorMessage = "Nome deve ter entre 2 e 100 caracteres.")]
    public string ClientName { get; init; } = string.Empty;

    [Required(ErrorMessage = "WhatsApp é obrigatório.")]
    [StringLength(20, MinimumLength = 10,
        ErrorMessage = "Telefone deve ter entre 10 e 20 caracteres.")]
    [RegularExpression(@"^\d+$",
        ErrorMessage = "Telefone deve conter apenas números.")]
    public string ClientPhone { get; init; } = string.Empty;

    [Required(ErrorMessage = "Data é obrigatória.")]
    [RegularExpression(@"^\d{4}-\d{2}-\d{2}$",
        ErrorMessage = "Data deve estar no formato YYYY-MM-DD.")]
    public string AppointmentDate { get; init; } = string.Empty;

    [Required(ErrorMessage = "Horário é obrigatório.")]
    [RegularExpression(@"^\d{2}:\d{2}$",
        ErrorMessage = "Horário deve estar no formato HH:mm.")]
    public string StartTime { get; init; } = string.Empty;
}