using Microsoft.EntityFrameworkCore;
using VetPax.Application.Modules.MedicationTreatment.Ports;
using VetPax.Infrastructure.Persistence;

namespace VetPax.Infrastructure.Modules.MedicationTreatment.Adapters;

public sealed class MedicationVeterinarianAdapter
    : IMedicationVeterinarianPort
{
    private readonly VetPaxDbContext _db;

    public MedicationVeterinarianAdapter(
        VetPaxDbContext db)
    {
        _db = db;
    }

    public async Task<bool> CanPrescribeAsync(
        Guid veterinarianId,
        Guid clinicId,
        Guid petId,
        CancellationToken cancellationToken = default)
    {
        if (veterinarianId == Guid.Empty ||
            clinicId == Guid.Empty ||
            petId == Guid.Empty)
        {
            return false;
        }

        var veterinarianIsActive =
            await _db.ClinicVeterinarians
                .AsNoTracking()
                .AnyAsync(
                    x => x.ClinicId == clinicId &&
                         x.VeterinarianId == veterinarianId &&
                         x.IsActive,
                    cancellationToken);

        if (!veterinarianIsActive)
            return false;

        var petIsLinked =
            await _db.PetClinicLinks
                .AsNoTracking()
                .AnyAsync(
                    x => x.PetId == petId &&
                         x.ClinicId == clinicId &&
                         x.IsActive,
                    cancellationToken);

        return petIsLinked;
    }
}