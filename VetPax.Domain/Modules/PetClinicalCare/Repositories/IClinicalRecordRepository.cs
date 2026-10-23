using VetPax.Domain.Modules.PetClinicalCare.Aggregates;
using VetPax.Domain.Modules.PetClinicalCare.ValueObjects;

namespace VetPax.Domain.Modules.PetClinicalCare.Repositories;

public interface IClinicalRecordRepository
{
    Task SaveAsync(
        ClinicalRecord clinicalRecord,
        CancellationToken cancellationToken = default
    );

    Task<ClinicalRecord?> FindByPetIdAsync(
        PetId petId,
        CancellationToken cancellationToken = default
    );
}