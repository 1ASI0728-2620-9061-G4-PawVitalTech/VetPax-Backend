using Microsoft.EntityFrameworkCore;
using VetPax.Application.Modules.PetClinicalCare.Ports;
using VetPax.Domain.Modules.PetClinicalCare.ValueObjects;
using VetPax.Infrastructure.Modules.PetClinicalCare.Persistence;
using VetPax.Infrastructure.Persistence;

namespace VetPax.Infrastructure.Modules.PetClinicalCare.Adapters;

public sealed class PetClinicLinkAdapter(VetPaxDbContext db)
    : IPetClinicLinkPort, IClinicDirectoryPort
{
    public Task<bool> ExistsAsync(
        Guid clinicId,
        CancellationToken cancellationToken = default)
    {
        return db.Clinics.AnyAsync(
            c => c.ClinicId == clinicId,
            cancellationToken);
    }

    public async Task LinkAsync(
        PetId petId,
        Guid clinicId,
        CancellationToken cancellationToken = default)
    {
        var link = await db.PetClinicLinks
            .SingleOrDefaultAsync(
                x => x.PetId == petId.Value &&
                     x.ClinicId == clinicId,
                cancellationToken);

        if (link is null)
        {
            db.PetClinicLinks.Add(new PetClinicLinkRow
            {
                PetId = petId.Value,
                ClinicId = clinicId,
                IsActive = true,
                LinkedAt = DateTime.UtcNow
            });
        }
        else
        {
            link.IsActive = true;
        }
    }
}