using BarberShop.Domain.Entities;
using BarberShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BarberShop.Application.Resolvers;

/// <summary>
/// Responsabilidade única: encontrar o BarberProfile de um usuário autenticado.
/// 
/// Por que existe essa classe?
/// Vários services precisam do mesmo comportamento: "dado um userId,
/// encontre o BarberProfile ou lance uma exceção clara". Em vez de
/// duplicar esse código em cada service, centralizamos aqui.
/// 
/// Onde é injetado?
/// Em qualquer service que precise do perfil do barbeiro autenticado.
/// </summary>
public class BarberProfileResolver(
    AppDbContext db,
    ILogger<BarberProfileResolver> logger)
{
    /// <summary>
    /// Busca o perfil do barbeiro pelo userId.
    /// Lança KeyNotFoundException se o perfil não existir.
    /// 
    /// Por que KeyNotFoundException?
    /// Porque o ErrorHandlingMiddleware já mapeia esse tipo de exceção
    /// para HTTP 404, mantendo consistência em toda a API.
    /// </summary>
    public async Task<BarberProfile> ResolveAsync(Guid userId)
    {
        var profile = await db.BarberProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile is null)
        {
            // Log interno com o userId para facilitar debugging
            // A mensagem para o cliente é genérica e clara
            logger.LogWarning(
                "Perfil não encontrado para UserId: {UserId}", userId);

            throw new KeyNotFoundException(
                "Perfil não encontrado. Configure seu perfil antes de continuar.");
        }

        return profile;
    }

    /// <summary>
    /// Busca o perfil pelo slug público (usado nos endpoints públicos).
    /// Lança KeyNotFoundException se o slug não existir.
    /// 
    /// Por que ter os dois métodos aqui e não em services separados?
    /// Porque ambos resolvem o mesmo problema — encontrar um BarberProfile —
    /// mas por chaves diferentes. Centralizar evita lógica de busca
    /// espalhada pela aplicação.
    /// </summary>
    public async Task<BarberProfile> ResolveBySlugAsync(string slug)
    {
        // Normaliza o slug antes de buscar
        var normalizedSlug = slug.Trim().ToLower();

        var profile = await db.BarberProfiles
            .FirstOrDefaultAsync(p => p.Slug == normalizedSlug);

        if (profile is null)
        {
            logger.LogWarning(
                "Perfil não encontrado para Slug: {Slug}", normalizedSlug);

            throw new KeyNotFoundException(
                "Barbeiro não encontrado.");
        }

        return profile;
    }
}