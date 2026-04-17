using BarberShop.Application.DTOs.Auth;
using BarberShop.Domain.Entities;
using BarberShop.Domain.Enums;
using BarberShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Logging;
using BarberShop.Application.Helpers;

namespace BarberShop.Application.Services;

public class AuthService(AppDbContext db, IConfiguration config, EmailService emailService, ILogger<AuthService> logger)
{

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var emailExists = await db.Users.AnyAsync(u => u.Email == request.Email.ToLower());
        if (emailExists)
            throw new InvalidOperationException("E-mail já cadastrado.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Email = request.Email.ToLower().Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            IsEmailConfirmed = true
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        // Tenta enviar o email mas não bloqueia o cadastro se falhar
        // try
        // {
        //     var token = await CreateEmailTokenAsync(user.Id, EmailTokenType.EmailConfirmation, hours: 24);
        //     await emailService.SendEmailConfirmationAsync(user.Email, user.Name, token);
        // }
        // catch (Exception ex)
        // {
        //     logger.LogWarning("Falha ao enviar email de confirmação para {Email}: {Error}", user.Email, ex.Message);
        // }

        return new AuthResponse
        {
            Token = GenerateToken(user),
            Name = user.Name,
            Email = user.Email,
            IsEmailConfirmed = user.IsEmailConfirmed
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        // var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email.ToLower());

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = Sanitizer.Email(request.Email),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            IsEmailConfirmed = true
        };

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("E-mail ou senha inválidos.");

        return new AuthResponse
        {
            Token = GenerateToken(user),
            Name = user.Name,
            Email = user.Email,
            IsEmailConfirmed = user.IsEmailConfirmed
        };
    }

    public async Task<AuthResponse> GetMeAsync(Guid userId)
    {
        var user = await db.Users.FindAsync(userId)
            ?? throw new KeyNotFoundException("Usuário não encontrado.");

        return new AuthResponse
        {
            Token = string.Empty,
            Name = user.Name,
            Email = user.Email,
            IsEmailConfirmed = user.IsEmailConfirmed
        };
    }

    public async Task ConfirmEmailAsync(string token)
    {
        var emailToken = await db.EmailTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t =>
                t.Token == token &&
                t.Type == EmailTokenType.EmailConfirmation)
            ?? throw new KeyNotFoundException("Token inválido.");

        if (!emailToken.IsValid())
            throw new InvalidOperationException("Token expirado ou já utilizado.");

        emailToken.User.IsEmailConfirmed = true;
        emailToken.UsedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        // Sempre retorna sucesso para não revelar se o e-mail existe
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = Sanitizer.Email(request.Email),
        };

        if (user is null) return;

        // Invalida tokens anteriores de reset para este usuário
        var oldTokens = await db.EmailTokens
            .Where(t => t.UserId == user.Id &&
                        t.Type == EmailTokenType.PasswordReset &&
                        t.UsedAt == null)
            .ToListAsync();

        foreach (var old in oldTokens)
            old.UsedAt = DateTime.UtcNow;

        var token = await CreateEmailTokenAsync(user.Id, EmailTokenType.PasswordReset, hours: 1);
        await emailService.SendPasswordResetAsync(user.Email, user.Name, token);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request)
    {
        if (request.NewPassword.Length < 6)
            throw new InvalidOperationException("A senha deve ter pelo menos 6 caracteres.");

        var emailToken = await db.EmailTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t =>
                t.Token == request.Token &&
                t.Type == EmailTokenType.PasswordReset)
            ?? throw new KeyNotFoundException("Token inválido.");

        if (!emailToken.IsValid())
            throw new InvalidOperationException("Token expirado ou já utilizado.");

        emailToken.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        emailToken.UsedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
    }

    public async Task ResendConfirmationAsync(Guid userId)
    {
        var user = await db.Users.FindAsync(userId)
            ?? throw new KeyNotFoundException("Usuário não encontrado.");

        if (user.IsEmailConfirmed)
            throw new InvalidOperationException("E-mail já confirmado.");

        try
        {
            var token = await CreateEmailTokenAsync(user.Id, EmailTokenType.EmailConfirmation, hours: 24);
            await emailService.SendEmailConfirmationAsync(user.Email, user.Name, token);
        }
        catch (Exception ex)
        {
            logger.LogWarning("Falha ao reenviar email para {Email}: {Error}", user.Email, ex.Message);
            throw new InvalidOperationException("Não foi possível enviar o email. Tente novamente mais tarde.");
        }
    }

    // Gera um token seguro e salva no banco
    private async Task<string> CreateEmailTokenAsync(Guid userId, EmailTokenType type, int hours)
    {
        var token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"); // 64 chars

        var emailToken = new EmailToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = token,
            Type = type,
            ExpiresAt = DateTime.UtcNow.AddHours(hours)
        };

        db.EmailTokens.Add(emailToken);
        await db.SaveChangesAsync();

        return token;
    }

    private string GenerateToken(User user)
    {
        var secret = config["Jwt:Secret"]!;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiration = int.Parse(config["Jwt:ExpirationDays"]!);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Name)
        };

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(expiration),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}