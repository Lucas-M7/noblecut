namespace BarberShop.Application.Helpers;

public static class DateTimeHelper
{
    // Fuso horário de Brasília (UTC-3)
    private static readonly TimeZoneInfo BrasiliaTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows()
                ? "E. South America Standard Time"
                : "America/Sao_Paulo");

    public static DateTime NowInBrasilia()
        => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, BrasiliaTimeZone);

    public static DateOnly TodayInBrasilia()
        => DateOnly.FromDateTime(NowInBrasilia());
}