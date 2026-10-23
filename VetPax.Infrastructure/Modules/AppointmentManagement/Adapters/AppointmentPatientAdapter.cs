
using Microsoft.EntityFrameworkCore;
using VetPax.Application.Modules.AppointmentManagement.Models;
using VetPax.Application.Modules.AppointmentManagement.Ports;
using VetPax.Domain.Modules.PetClinicalCare.ValueObjects;
using VetPax.Infrastructure.Persistence;

namespace VetPax.Infrastructure.Modules.AppointmentManagement.Adapters;

public sealed class AppointmentPatientAdapter
    : IAppointmentPatientPort
{
    private readonly VetPaxDbContext _db;

    public AppointmentPatientAdapter(VetPaxDbContext db)
    {
        _db = db;
    }

    public async Task<bool> IsOwnedAndLinkedAsync(
        Guid petId,
        Guid ownerId,
        Guid clinicId,
        CancellationToken cancellationToken = default)
    {
        if (petId == Guid.Empty ||
            ownerId == Guid.Empty ||
            clinicId == Guid.Empty)
        {
            return false;
        }

        var belongsToOwner = await _db.Pets
            .AsNoTracking()
            .AnyAsync(
                p => p.PetId == new PetId(petId) &&
                     p.OwnerId == new OwnerId(ownerId),
                cancellationToken);

        if (!belongsToOwner)
            return false;

        return await _db.PetClinicLinks
            .AsNoTracking()
            .AnyAsync(
                link => link.PetId == petId &&
                        link.ClinicId == clinicId &&
                        link.IsActive,
                cancellationToken);
    }

    public async Task<
        IReadOnlyDictionary<Guid, AppointmentPatientSummary>>
        GetSummariesAsync(
            IReadOnlyCollection<Guid> petIds,
            CancellationToken cancellationToken = default)
    {
        if (petIds.Count == 0)
        {
            return new Dictionary<
                Guid, AppointmentPatientSummary>();
        }

        var result =
            new Dictionary<Guid, AppointmentPatientSummary>();

        // Utilizamos consultas por identificador para
        // mantener compatibilidad con el ValueConverter
        // PetId de PetClinicalCare.
        foreach (var id in petIds.Distinct())
        {
            var pet = await _db.Pets
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.PetId == new PetId(id),
                    cancellationToken);

            if (pet is null)
                continue;

            result[id] = new AppointmentPatientSummary(
                id,
                pet.Name,
                pet.Species.ToString());
        }

        return result;
    }
}
