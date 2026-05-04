using BarberShop.Application.DTOs.Blocks;
using BarberShop.Application.Resolvers;
using BarberShop.Domain.Entities;
using BarberShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BarberShop.Application.Services;

public class BlockService(AppDbContext db,
    BarberProfileResolver profileResolver)
{
    public async Task<List<BlockResponse>> GetAsync(Guid userId)
    {
        var profile = await db.BarberProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile is null)
            return [];

        return await db.ScheduleBlocks
            .Where(b => b.BarberProfileId == profile.Id)
            .OrderBy(b => b.StartDate)
            .Select(b => ToResponse(b))
            .ToListAsync();
    }

    public async Task<BlockResponse> CreateAsync(Guid userId, CreateBlockRequest request)
    {
        if (!DateOnly.TryParse(request.StartDate, out var start))
            throw new InvalidOperationException("Data de início inválida.");

        if (!DateOnly.TryParse(request.EndDate, out var end))
            throw new InvalidOperationException("Data de fim inválida.");

        if (end < start)
            throw new InvalidOperationException("A data de fim não pode ser anterior à data de início.");

        var profile = await profileResolver.ResolveAsync(userId);

        var block = new ScheduleBlock
        {
            Id = Guid.NewGuid(),
            BarberProfileId = profile.Id,
            StartDate = start,
            EndDate = end,
            Reason = request.Reason?.Trim()
        };

        db.ScheduleBlocks.Add(block);
        await db.SaveChangesAsync();

        return ToResponse(block);
    }

    public async Task DeleteAsync(Guid userId, Guid blockId)
    {
        var profile = await profileResolver.ResolveAsync(userId);

        var block = await db.ScheduleBlocks
            .FirstOrDefaultAsync(b => b.Id == blockId && b.BarberProfileId == profile.Id)
            ?? throw new KeyNotFoundException("Bloqueio não encontrado.");

        db.ScheduleBlocks.Remove(block);
        await db.SaveChangesAsync();
    }

    private static BlockResponse ToResponse(ScheduleBlock b) => new()
    {
        Id = b.Id,
        StartDate = b.StartDate.ToString("yyyy-MM-dd"),
        EndDate = b.EndDate.ToString("yyyy-MM-dd"),
        Reason = b.Reason
    };
}