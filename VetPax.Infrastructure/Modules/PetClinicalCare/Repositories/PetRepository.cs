using Microsoft.EntityFrameworkCore;
using VetPax.Domain.Modules.PetClinicalCare.Aggregates;
using VetPax.Domain.Modules.PetClinicalCare.Repositories;
using VetPax.Domain.Modules.PetClinicalCare.ValueObjects;
using VetPax.Infrastructure.Persistence;

namespace VetPax.Infrastructure.Modules.PetClinicalCare.Repositories;

public sealed class PetRepository : IPetRepository
{
    private readonly VetPaxDbContext _context;

    public PetRepository(VetPaxDbContext context)
    {
        _context = context;
    }

    public async Task SaveAsync(
        Pet pet,
        CancellationToken cancellationToken = default)
    {
        await _context.Set<Pet>()
            .AddAsync(pet, cancellationToken);
    }

    public async Task<Pet?> FindByIdAsync(
        PetId petId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<Pet>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.PetId == petId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Pet>> FindByOwnerIdAsync(
        OwnerId ownerId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<Pet>()
            .AsNoTracking()
            .Where(p => p.OwnerId == ownerId)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByIdAsync(
        PetId petId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<Pet>()
            .AnyAsync(
                p => p.PetId == petId,
                cancellationToken);
    }
}