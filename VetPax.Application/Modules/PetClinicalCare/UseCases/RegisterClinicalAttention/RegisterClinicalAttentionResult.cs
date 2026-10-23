namespace VetPax.Application.Modules.PetClinicalCare.UseCases.RegisterClinicalAttention;

public sealed record RegisterClinicalAttentionResult(
    Guid AttentionId,
    Guid ClinicalRecordId,
    Guid PetId
);