using BarberShop.Application.DTOs.Auth;
using BarberShop.Domain.Entities;
using BarberShop.Domain.Enums;
using BarberShop.Infrastructure.Data;
using Google.Apis.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BarberShop.Application.Services;

public class AuthService(
    AppDbContext db,
    IConfiguration config,
    EmailService emailService,
    ILogger<AuthService> logger)
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        // Normaliza o email antes de verificar duplicata
        var normalizedEmail = request.Email.Trim().ToLower();

        var emailExists = await db.Users.AnyAsync(u => u.Email == normalizedEmail);
        if (emailExists)
            throw new InvalidOperationException("E-mail já cadastrado.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Email = normalizedEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            IsEmailConfirmed = false
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        logger.LogInformation("Novo usuário cadastrado. UserId: {UserId}", user.Id);

        await SendConfirmationEmailInternalAsync(user);

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
        var normalizedEmail = request.Email.Trim().ToLower();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);

        // Proteção contra enumeração de usuários via timing attack:
        if (user is null)
        {
            BCrypt.Net.BCrypt.Verify(
                request.Password,
                "$2a$11$dummyhashtopreventtimingXXXXXXXXXXXXXXXXXXXXXXXXXXXXX");

            // Log interno usa termo neutro — não revela se email existe
            logger.LogWarning(
                "Falha de autenticação. Nenhuma conta encontrada para o email informado.");

            throw new UnauthorizedAccessException("E-mail ou senha inválidos.");
        }

        // Usuário existe — verifica a senha normalmente
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            logger.LogWarning(
                "Falha de autenticação. Senha incorreta para UserId: {UserId}", user.Id);

            throw new UnauthorizedAccessException("E-mail ou senha inválidos.");
        }

        logger.LogInformation("Login bem-sucedido. UserId: {UserId}", user.Id);

        return new AuthResponse
        {
            Token = GenerateToken(user),
            Name = user.Name,
            Email = user.Email,
            IsEmailConfirmed = user.IsEmailConfirmed
        };
    }

    public async Task<AuthResponse> GoogleLoginAsync(GoogleAuthRequest request)
    {
        GoogleJsonWebSignature.Payload payload;
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = [config["Google:ClientId"]!]
            };
            payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, settings);
        }
        catch (InvalidJwtException ex)
        {
            logger.LogWarning("Token Google inválido: {message}", ex.Message);
            throw new UnauthorizedAccessException("Token do Google inválido ou expirado.");
        }

        var email = payload.Email.Trim().ToLower();
        var name = !string.IsNullOrWhiteSpace(payload.Name)
            ? payload.Name.Trim()
            : email.Split('@')[0]; // fallback caso o Google não retorne o nome

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user is null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                Name = name,
                Email = email,
                PasswordHash = null,
                IsEmailConfirmed = true
            };

            db.Users.Add(user);
            await db.SaveChangesAsync();

            logger.LogInformation(
                "Login via Google para usuário existente. UserId: {UserId}", user.Id);
        }

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

        logger.LogInformation(
            "Email confirmado. UserId: {UserId}", emailToken.UserId);
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        var normalizedEmail = request.Email.Trim().ToLower();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);

        // Retorna silenciosamente se email não existe
        if (user is null)
        {
            logger.LogWarning("ForgotPassword: email não cadastrado solicitou reset.");
            return;
        }

        if (user.PasswordHash is null)
        {
            logger.LogWarning(
                "ForgotPassword ignorado, conta Google sem senha. UserId: {UserId}", user.Id);
            return;
        }

        var oldTokens = await db.EmailTokens
            .Where(t => t.UserId == user.Id &&
                        t.Type == EmailTokenType.PasswordReset &&
                        t.UsedAt == null)
            .ToListAsync();

        foreach (var old in oldTokens)
            old.UsedAt = DateTime.UtcNow;

        var token = await CreateEmailTokenAsync(user.Id, EmailTokenType.PasswordReset, hours: 1);

        try
        {
            var resetLink = BuildLink("reset-password", token);
            await emailService.SendPasswordResetAsync(user.Email, user.Name, resetLink);
        }
        catch (Exception ex)
        {
            logger.LogWarning("Falha ao enviar email de reset. UserId: {UserId}. Erro: {Error}",
                user.Id, ex.Message);
        }
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request)
    {
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

        logger.LogInformation("Senha redefinida com sucesso. UserId: {UserId}", emailToken.UserId);
    }

    // public async Task ResendConfirmationAsync(Guid userId)
    // {
    //     var user = await db.Users.FindAsync(userId)
    //         ?? throw new KeyNotFoundException("Usuário não encontrado.");

    //     if (user.IsEmailConfirmed)
    //         throw new InvalidOperationException("E-mail já confirmado.");

    //     try
    //     {
    //         var token = await CreateEmailTokenAsync(user.Id, EmailTokenType.EmailConfirmation, hours: 24);
    //         await emailService.SendEmailConfirmationAsync(user.Email, user.Name, token);
    //     }
    //     catch (Exception ex)
    //     {
    //         logger.LogWarning("Falha ao reenviar confirmação. UserId: {UserId}. Erro: {Error}",
    //             user.Id, ex.Message);
    //         throw new InvalidOperationException("Não foi possível enviar o email. Tente novamente.");
    //     }
    // }

    private async Task SendConfirmationEmailInternalAsync(User user)
    {
        try
        {
            var token = await CreateEmailTokenAsync(
                user.Id, EmailTokenType.EmailConfirmation, hours: 24);

            var confirmLink = BuildLink("confirm-email", token);
            await emailService.SendEmailConfirmationAsync(user.Email, user.Name, confirmLink);
        }
        catch (Exception ex)
        {
            logger.LogWarning("Falha ao enviar confirmação de email. UserId: {UserId}. Erro: {Error}",
            user.Id, ex.Message);
        }
    }

    private async Task<string> CreateEmailTokenAsync(Guid userId, EmailTokenType type, int hours)
    {
        var token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");

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

    /// <summary>
    /// Monta a URL do frontend para links de email.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    private string BuildLink(string path, string token)
    {
        var frontendUrl = config["App:FrontendUrl"]!.TrimEnd('/');
        return $"{frontendUrl}/{path}?token={token}";
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