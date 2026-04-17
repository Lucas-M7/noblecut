using System.ComponentModel.DataAnnotations;

namespace BarberShop.Application.DTOs.SpecialHours;

public class UpsertSpecialHourRequest
{
    [Required(ErrorMessage = "Data é obrigatória.")]
    [RegularExpression(@"^\d{4}-\d{2}-\d{2}$", ErrorMessage = "Data deve estar no formato YYYY-MM-DD.")]
    public string Date { get; set; } = string.Empty;

    public bool IsOpen { get; set; }

    [RegularExpression(@"^\d{2}:\d{2}$", ErrorMessage = "Horário de início deve estar no formato HH:mm.")]
    public string StartTime { get; set; } = "09:00";

    [RegularExpression(@"^\d{2}:\d{2}$", ErrorMessage = "Horário de fim deve estar no formato HH:mm.")]
    public string EndTime { get; set; } = "18:00";

    public bool HasLunchBreak { get; set; }

    [RegularExpression(@"^\d{2}:\d{2}$", ErrorMessage = "Horário de início do almoço deve estar no formato HH:mm.")]
    public string? LunchStart { get; set; }

    [RegularExpression(@"^\d{2}:\d{2}$", ErrorMessage = "Horário de fim do almoço deve estar no formato HH:mm.")]
    public string? LunchEnd { get; set; }

    [StringLength(255, ErrorMessage = "Motivo deve ter no máximo 255 caracteres.")]
    public string? Reason { get; set; }
}