using BarberShop.Application.DTOs.Services;
using BarberShop.Application.Resolvers;
using BarberShop.Domain.Entities;
using BarberShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BarberShop.Application.Services;

public class ServiceService(AppDbContext db,
    BarberProfileResolver profileResolver)
{
    public async Task<List<ServiceResponse>> GetAllAsync(Guid userId)
    {
        var profile = await db.BarberProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);

        // Se não tiver perfil ainda, retorna lista vazia em vez de lançar erro
        // Comportamento intencional, pois barbeiro novo cadastro no sistema
        // Ainda não tem serviços
        if (profile is null) return [];

        return await db.Services
            .Where(s => s.BarberProfileId == profile.Id)
            .OrderBy(s => s.Name)
            .Select(s => ToResponse(s))
            .ToListAsync();
    }

    public async Task<ServiceResponse> CreateAsync(Guid userId, CreateServiceRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("Nome do serviço é obrigatório.");

        if (request.DurationMinutes <= 0)
            throw new InvalidOperationException("Duração deve ser maior que zero.");

        // Aqui o perfil deve existir para criar serviço
        var profile = await profileResolver.ResolveAsync(userId);

        var service = new Service
        {
            Id = Guid.NewGuid(),
            BarberProfileId = profile.Id,
            Name = request.Name.Trim(),
            DurationMinutes = request.DurationMinutes,
            Price = request.Price,
            IsActive = true
        };

        db.Services.Add(service);
        await db.SaveChangesAsync();

        return ToResponse(service);
    }

    public async Task<ServiceResponse> UpdateAsync(Guid userId, Guid serviceId, UpdateServiceRequest request)
    {
        var service = await GetOwnedServiceAsync(userId, serviceId);

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("Nome do serviço é obrigatório.");

        if (request.DurationMinutes <= 0)
            throw new InvalidOperationException("Duração deve ser maior que zero.");

        service.Name = request.Name.Trim();
        service.DurationMinutes = request.DurationMinutes;
        service.Price = request.Price;

        await db.SaveChangesAsync();
        return ToResponse(service);
    }

    public async Task<ServiceResponse> ToggleAsync(Guid userId, Guid serviceId)
    {
        var service = await GetOwnedServiceAsync(userId, serviceId);
        service.IsActive = !service.IsActive;
        await db.SaveChangesAsync();
        return ToResponse(service);
    }

    private async Task<Service> GetOwnedServiceAsync(Guid userId, Guid serviceId)
    {
        var profile = await profileResolver.ResolveAsync(userId);

        return await db.Services
            .FirstOrDefaultAsync(s => s.Id == serviceId && s.BarberProfileId == profile.Id)
            ?? throw new KeyNotFoundException("Serviço não encontrado.");
    }

    private static ServiceResponse ToResponse(Service s) => new()
    {
        Id = s.Id,
        Name = s.Name,
        DurationMinutes = s.DurationMinutes,
        Price = s.Price,
        IsActive = s.IsActive
    };
}