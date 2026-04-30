using System.ComponentModel.DataAnnotations;

namespace BarberShop.Application.DTOs.SpecialHours;

public record UpsertSpecialHourRequest
{
    [Required(ErrorMessage = "Data é obrigatória.")]
    [RegularExpression(@"^\d{4}-\d{2}-\d{2}$",
        ErrorMessage = "Data deve estar no formato YYYY-MM-DD.")]
    public string Date { get; init; } = string.Empty;

    public bool IsOpen { get; init; }

    [RegularExpression(@"^\d{2}:\d{2}$",
        ErrorMessage = "Horário de início deve estar no formato HH:mm.")]
    public string StartTime { get; init; } = "09:00";

    [RegularExpression(@"^\d{2}:\d{2}$",
        ErrorMessage = "Horário de fim deve estar no formato HH:mm.")]
    public string EndTime { get; init; } = "18:00";

    public bool HasLunchBreak { get; init; }

    [RegularExpression(@"^\d{2}:\d{2}$",
        ErrorMessage = "Horário de início do almoço deve estar no formato HH:mm.")]
    public string? LunchStart { get; init; }

    [RegularExpression(@"^\d{2}:\d{2}$",
        ErrorMessage = "Horário de fim do almoço deve estar no formato HH:mm.")]
    public string? LunchEnd { get; init; }

    [StringLength(255, ErrorMessage = "Motivo deve ter no máximo 255 caracteres.")]
    public string? Reason { get; init; }
}