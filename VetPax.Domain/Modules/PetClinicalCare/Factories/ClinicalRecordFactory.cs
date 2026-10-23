using VetPax.Domain.Modules.PetClinicalCare.Aggregates;
using VetPax.Domain.Modules.PetClinicalCare.ValueObjects;

namespace VetPax.Domain.Modules.PetClinicalCare.Factories;

public static class ClinicalRecordFactory
{
    public static ClinicalRecord OpenFor(PetId petId)
    {
        return new ClinicalRecord(
            ClinicalRecordId.New(),
            petId
        );
    }
}