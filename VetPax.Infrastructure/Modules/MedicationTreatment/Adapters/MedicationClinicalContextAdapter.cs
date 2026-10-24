using VetPax.Application.Modules.MedicationTreatment.Ports;
using VetPax.Domain.Modules.PetClinicalCare.Repositories;
using VetPax.Domain.Modules.PetClinicalCare.ValueObjects;

namespace VetPax.Infrastructure.Modules.MedicationTreatment.Adapters;

public sealed class MedicationClinicalContextAdapter
    : IMedicationClinicalContextPort
{
    private readonly IClinicalRecordRepository _clinicalRecords;

    public MedicationClinicalContextAdapter(
        IClinicalRecordRepository clinicalRecords)
    {
        _clinicalRecords = clinicalRecords;
    }

    public async Task<bool> MatchesAsync(
        Guid clinicalAttentionId,
        Guid petId,
        Guid clinicId,
        Guid veterinarianId,
        CancellationToken cancellationToken = default)
    {
        if (clinicalAttentionId == Guid.Empty ||
            petId == Guid.Empty ||
            clinicId == Guid.Empty ||
            veterinarianId == Guid.Empty)
        {
            return false;
        }

        var clinicalRecord =
            await _clinicalRecords.FindByPetIdAsync(
                new PetId(petId),
                cancellationToken);

        if (clinicalRecord is null)
            return false;

        return clinicalRecord.Attentions.Any(attention =>
            attention.AttentionId.Value == clinicalAttentionId &&
            attention.ClinicId.Value == clinicId &&
            attention.VeterinarianId.Value == veterinarianId);
    }
}