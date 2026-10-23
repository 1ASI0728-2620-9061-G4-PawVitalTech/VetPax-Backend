using VetPax.Domain.Modules.PetClinicalCare.ValueObjects;

namespace VetPax.Application.Modules.PetClinicalCare.UseCases.RegisterPet;

public sealed record RegisterPetResult(
    PetId PetId,
    ClinicalRecordId ClinicalRecordId
);