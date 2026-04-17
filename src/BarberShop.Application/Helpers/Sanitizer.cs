using System.Text.RegularExpressions;

namespace BarberShop.Application.Helpers;

public class Sanitizer
{
    // Remove espaços extras nas bordas
    public static string Trim(string value) =>
        value.Trim();

    // Normaliza para lowercase e remove espaços
    public static string Email(string value) =>
        value.Trim().ToLower();

    // Gera slug válido a partir de qualquer string
    public static string Slug(string value) =>
        value.Trim().ToLower()
            .Replace(" ", "-");

    // Remove tudo que não é número de telefone
    public static string Phone(string value) =>
        Regex.Replace(value, @"\D", "");
}