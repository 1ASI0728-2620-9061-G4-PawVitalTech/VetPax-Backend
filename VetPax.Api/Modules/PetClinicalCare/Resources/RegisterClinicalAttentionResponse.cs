namespace VetPax.Api.Modules.PetClinicalCare.Resources;

public sealed record RegisterClinicalAttentionResponse(
    Guid AttentionId,
    Guid ClinicalRecordId,
    Guid PetId
);