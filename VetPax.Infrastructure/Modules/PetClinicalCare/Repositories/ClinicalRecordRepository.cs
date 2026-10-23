
using Microsoft.EntityFrameworkCore;
using VetPax.Domain.Modules.PetClinicalCare.Aggregates;
using VetPax.Domain.Modules.PetClinicalCare.Repositories;
using VetPax.Domain.Modules.PetClinicalCare.ValueObjects;
using VetPax.Infrastructure.Persistence;

namespace VetPax.Infrastructure.Modules.PetClinicalCare.Repositories;

public sealed class ClinicalRecordRepository
    : IClinicalRecordRepository
{
    private readonly VetPaxDbContext _context;

    public ClinicalRecordRepository(VetPaxDbContext context)
    {
        _context = context;
    }

    public async Task SaveAsync(
        ClinicalRecord clinicalRecord,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(clinicalRecord);

        var entry = _context.Entry(clinicalRecord);

        if (entry.State == EntityState.Detached)
        {
            // A detached aggregate must be a new record.
            // Existing records must first be loaded by this repository.
            var exists = await _context.ClinicalRecords
                .AnyAsync(
                    r => r.ClinicalRecordId ==
                         clinicalRecord.ClinicalRecordId,
                    cancellationToken);

            if (exists)
                throw new InvalidOperationException(
                    "Load the existing clinical record before updating it.");

            await _context.ClinicalRecords.AddAsync(
                clinicalRecord,
                cancellationToken);
        }

        // Existing tracked records are saved by UnitOfWork.
    }

    public async Task<ClinicalRecord?> FindByPetIdAsync(
        PetId petId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ClinicalRecords
            .AsTracking()
            .Include(r => r.Attentions)
                .ThenInclude(a => a.Indicators)
            .AsSplitQuery()
            .SingleOrDefaultAsync(
                r => r.PetId == petId,
                cancellationToken);
    }
}
