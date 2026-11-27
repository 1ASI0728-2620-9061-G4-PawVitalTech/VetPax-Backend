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

    public async Task<bool> HasActiveVeterinarianAccessToPetAsync(
        Guid veterinarianId,
        Guid petId,
        CancellationToken cancellationToken = default)
    {
        if (veterinarianId == Guid.Empty || petId == Guid.Empty)
            return false;

        return await (
                from membership in _db.ClinicVeterinarians.AsNoTracking()
                join link in _db.PetClinicLinks.AsNoTracking()
                    on membership.ClinicId equals link.ClinicId
                where membership.VeterinarianId == veterinarianId
                      && membership.IsActive
                      && link.PetId == petId
                      && link.IsActive
                select membership.ClinicId)
            .AnyAsync(cancellationToken);
    }
}