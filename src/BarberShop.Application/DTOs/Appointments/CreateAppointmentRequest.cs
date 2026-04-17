using System.ComponentModel.DataAnnotations;

namespace BarberShop.Application.DTOs.Appointments;

public class CreateAppointmentRequest
{
    [Required(ErrorMessage = "Serviço é obrigatório.")]
    public Guid ServiceId { get; set; }

    [Required(ErrorMessage = "Nome do cliente é obrigatório.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Nome deve ter entre 2 e 100 caracteres.")]
    public string ClientName { get; set; } = string.Empty;

    [Required(ErrorMessage = "WhatsApp é obrigatório.")]
    [StringLength(20, MinimumLength = 10, ErrorMessage = "Telefone deve ter entre 10 e 20 caracteres.")]
    [RegularExpression(@"^\d+$", ErrorMessage = "Telefone deve conter apenas números.")]
    public string ClientPhone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Data é obrigatória.")]
    [RegularExpression(@"^\d{4}-\d{2}-\d{2}$", ErrorMessage = "Data deve estar no formato YYYY-MM-DD.")]
    public string AppointmentDate { get; set; } = string.Empty;

    [Required(ErrorMessage = "Horário é obrigatório.")]
    [RegularExpression(@"^\d{2}:\d{2}$", ErrorMessage = "Horário deve estar no formato HH:mm.")]
    public string StartTime { get; set; } = string.Empty;
}