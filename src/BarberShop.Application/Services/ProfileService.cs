using BarberShop.Application.DTOs.Profile;
using BarberShop.Domain.Entities;
using BarberShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BarberShop.Application.Services;

public class ProfileService(
    AppDbContext db,
    CloudinaryService cloudinaryService,
    ILogger<ProfileService> logger)
{
    public async Task<ProfileResponse> GetAsync(Guid userId)
    {
        var profile = await db.BarberProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId)
            ?? throw new KeyNotFoundException("Perfil não encontrado.");

        return ToResponse(profile);
    }

    public async Task<ProfileResponse> UpsertAsync(Guid userId, UpdateProfileRequest request)
    {
        var slug = request.Slug.ToLower().Trim();

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
                Slug = slug,
                PrimaryColor = request.PrimaryColor
            };
            db.BarberProfiles.Add(profile);
        }
        else
        {
            profile.DisplayName = request.DisplayName.Trim();
            profile.BusinessName = request.BusinessName.Trim();
            profile.Phone = request.Phone.Trim();
            profile.Slug = slug;
            profile.PrimaryColor = request.PrimaryColor;
        }

        await db.SaveChangesAsync();

        logger.LogInformation("Perfil atualizado. UserId: {UserId}", userId);
        return ToResponse(profile);
    }

    public async Task<ProfileResponse> UpdatePhotoAsync(Guid userId, Stream imageStream, string fileName)
    {
        // Valida extensão antes de fazer upload
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var extension = Path.GetExtension(fileName).ToLower();

        if (!allowedExtensions.Contains(extension))
            throw new InvalidOperationException(
                "Formato inválido. Use JPG, PNG ou WebP.");

        var profile = await db.BarberProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId)
            ?? throw new KeyNotFoundException("Perfil não encontrado. Configure seu perfil antes de adicionar uma foto.");

        // Remove a foto antiga se existir
        if (!string.IsNullOrEmpty(profile.PhotoUrl))
            await cloudinaryService.DeleteAsync(profile.PhotoUrl);

        // Faz upload da nova foto
        var photoUrl = await cloudinaryService.UploadProfilePhotoAsync(imageStream, fileName);

        profile.PhotoUrl = photoUrl;
        await db.SaveChangesAsync();

        logger.LogInformation("Foto atualizada. UserId: {UserId}", userId);
        return ToResponse(profile);
    }

    public async Task<ProfileResponse> RemovePhotoAsync(Guid userId)
    {
        var profile = await db.BarberProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId)
            ?? throw new KeyNotFoundException("Perfil não encontrado.");

        if (!string.IsNullOrEmpty(profile.PhotoUrl))
        {
            await cloudinaryService.DeleteAsync(profile.PhotoUrl);
            profile.PhotoUrl = null;
            await db.SaveChangesAsync();
        }

        return ToResponse(profile);
    }

    private static ProfileResponse ToResponse(BarberProfile p) => new()
    {
        Id = p.Id,
        DisplayName = p.DisplayName,
        BusinessName = p.BusinessName,
        Phone = p.Phone,
        Slug = p.Slug,
        PhotoUrl = p.PhotoUrl,
        PrimaryColor = p.PrimaryColor
    };
}