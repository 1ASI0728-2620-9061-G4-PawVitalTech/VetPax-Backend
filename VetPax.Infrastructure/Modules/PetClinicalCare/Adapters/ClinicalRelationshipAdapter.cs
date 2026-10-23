using Microsoft.EntityFrameworkCore;
using VetPax.Application.Modules.PetClinicalCare.Ports;
using VetPax.Infrastructure.Persistence;

namespace VetPax.Infrastructure.Modules.PetClinicalCare.Adapters;

public sealed class ClinicalRelationshipAdapter
    : IClinicalRelationshipPort
{
    private readonly VetPaxDbContext _db;

    public ClinicalRelationshipAdapter(VetPaxDbContext db)
    {
        _db = db;
    }

    public async Task<bool> HasActiveVeterinarianMembershipAsync(
        Guid veterinarianId,
        Guid clinicId,
        CancellationToken cancellationToken = default)
    {
        if (veterinarianId == Guid.Empty ||
            clinicId == Guid.Empty)
        {
            return false;
        }

        return await _db.ClinicVeterinarians
            .AsNoTracking()
            .AnyAsync(
                x => x.VeterinarianId == veterinarianId
                     && x.ClinicId == clinicId
                     && x.IsActive,
                cancellationToken);
    }

    public async Task<bool> HasActivePatientLinkAsync(
        Guid petId,
        Guid clinicId,
        CancellationToken cancellationToken = default)
    {
        if (petId == Guid.Empty ||
            clinicId == Guid.Empty)
        {
            return false;
        }

        return await _db.PetClinicLinks
            .AsNoTracking()
            .AnyAsync(
                x => x.PetId == petId
                     && x.ClinicId == clinicId
                     && x.IsActive,
                cancellationToken);
    }
}