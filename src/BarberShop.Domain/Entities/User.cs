namespace BarberShop.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    // nullable pois contas criadas via Google não têm senha local
    public string? PasswordHash { get; set; } = string.Empty;
    public bool IsEmailConfirmed { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public BarberProfile? BarberProfile { get; set; }
    public ICollection<EmailToken> EmailTokens { get; set; } = [];
}