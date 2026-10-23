namespace VetPax.Application.Modules.PetClinicalCare.UseCases.GetClinicalHistory;

public sealed record GetClinicalHistoryResult(
    Guid PetId,
    Guid ClinicalRecordId,
    DateTime LastUpdatedAt,
    IReadOnlyList<ClinicalAttentionResult> Attentions
);

public sealed record ClinicalAttentionResult(
    Guid AttentionId,
    Guid ClinicId,
    Guid VeterinarianId,
    DateTime AttentionDate,
    string Reason,
    string Diagnosis,
    string TreatmentSummary,
    string? Observations,
    IReadOnlyList<ClinicalIndicatorResult> Indicators
);

public sealed record ClinicalIndicatorResult(
    string Name,
    decimal Value,
    string Unit
);