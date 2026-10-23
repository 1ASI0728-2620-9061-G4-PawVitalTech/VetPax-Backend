namespace VetPax.Api.Modules.PetClinicalCare.Resources;

public sealed record RegisterPetResponse(
    Guid PetId,
    Guid ClinicalRecordId
);