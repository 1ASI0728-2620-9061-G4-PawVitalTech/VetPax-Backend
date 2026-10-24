using VetPax.Application.Modules.MedicationTreatment.Ports;
using VetPax.Domain.Modules.PetClinicalCare.Repositories;
using VetPax.Domain.Modules.PetClinicalCare.ValueObjects;

namespace VetPax.Infrastructure.Modules.MedicationTreatment.Adapters;

public sealed class MedicationPatientAdapter
    : IMedicationPatientPort
{
    private readonly IPetRepository _pets;

    public MedicationPatientAdapter(IPetRepository pets)
    {
        _pets = pets;
    }

    public async Task<Guid?> GetOwnerIdAsync(
        Guid petId,
        CancellationToken cancellationToken = default)
    {
        if (petId == Guid.Empty)
            return null;

        var pet = await _pets.FindByIdAsync(
            new PetId(petId),
            cancellationToken);

        return pet?.OwnerId.Value;
    }
}