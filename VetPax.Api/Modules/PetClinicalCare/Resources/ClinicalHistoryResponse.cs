using VetPax.Application.Modules.PetClinicalCare.UseCases.GetClinicalHistory;

namespace VetPax.Api.Modules.PetClinicalCare.Resources;

public sealed record ClinicalHistoryResponse(
    Guid PetId,
    Guid ClinicalRecordId,
    DateTime LastUpdatedAt,
    string? Message,
    IReadOnlyList<ClinicalAttentionResult> Attentions
);