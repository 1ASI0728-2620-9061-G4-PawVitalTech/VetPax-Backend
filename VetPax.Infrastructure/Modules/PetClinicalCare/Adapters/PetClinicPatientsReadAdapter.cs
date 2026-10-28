using Microsoft.EntityFrameworkCore;
using VetPax.Application.Modules.PetClinicalCare.Ports;
using VetPax.Domain.Modules.PetClinicalCare.ValueObjects;
using VetPax.Infrastructure.Persistence;

namespace VetPax.Infrastructure.Modules.PetClinicalCare.Adapters;

public sealed class PetClinicPatientsReadAdapter(
    VetPaxDbContext db) : IPetClinicPatientReadPort
{
    public async Task<IReadOnlyList<LinkedClinicPatient>> FindByClinicAsync(
        Guid clinicId,
        CancellationToken cancellationToken = default)
    {
        var ids = await db.PetClinicLinks
            .AsNoTracking()
            .Where(x => x.ClinicId == clinicId && x.IsActive)
            .Select(x => x.PetId)
            .ToListAsync(cancellationToken);

        var patients = new List<LinkedClinicPatient>();

        foreach (var id in ids.Distinct())
        {
            var petId = new PetId(id);

            var pet = await db.Pets
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.PetId == petId,
                    cancellationToken);

            if (pet is null)
                continue;

            var record = await db.ClinicalRecords
                .AsNoTracking()
                .Include(x => x.Attentions)
                .AsSplitQuery()
                .SingleOrDefaultAsync(
                    x => x.PetId == petId,
                    cancellationToken);

            var lastAttention = record?.Attentions
                .Where(a => a.ClinicId.Value == clinicId)
                .Select(a => (DateTime?)a.AttentionDate)
                .Max();

            patients.Add(new LinkedClinicPatient(
                id,
                pet.Name,
                pet.Species.ToString(),
                lastAttention));
        }

        return patients
            .OrderBy(x => x.Name)
            .ToArray();
    }
}