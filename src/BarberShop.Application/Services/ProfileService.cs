using BarberShop.Application.DTOs.Profile;
using BarberShop.Application.Helpers;
using BarberShop.Domain.Entities;
using BarberShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BarberShop.Application.Services;

public class ProfileService(AppDbContext db)
{
    public async Task<ProfileResponse> GetAsync(Guid userId)
    {
        var profile = await db.BarberProfiles.FirstOrDefaultAsync(p => p.UserId == userId)
            ?? throw new KeyNotFoundException("Perfil não encontrado.");

        return ToResponse(profile);
    }

    public async Task<ProfileResponse> UpsertAsync(Guid userId, UpdateProfileRequest request)
    {
        var slug = Sanitizer.Slug(request.Slug);

        var slugTaken = await db.BarberProfiles
            .AnyAsync(p => p.Slug == slug && p.UserId != userId);

        if (slugTaken)
            throw new InvalidOperationException("Este slug já está em uso.");

        var profile = await db.BarberProfiles.FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile is null)
        {
            profile = new BarberProfile
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                DisplayName = request.DisplayName.Trim(),
                BusinessName = request.BusinessName.Trim(),
                Phone = request.Phone.Trim(),
                Slug = slug
            };
            db.BarberProfiles.Add(profile);
        }
        else
        {
            profile.DisplayName = Sanitizer.Trim(request.DisplayName);
            profile.BusinessName = Sanitizer.Trim(request.BusinessName);
            profile.Phone = Sanitizer.Phone(request.Phone);
            profile.Slug = slug;
        }

        await db.SaveChangesAsync();
        return ToResponse(profile);
    }

    private static ProfileResponse ToResponse(BarberProfile p) => new()
    {
        Id = p.Id,
        DisplayName = p.DisplayName,
        BusinessName = p.BusinessName,
        Phone = p.Phone,
        Slug = p.Slug
    };
}