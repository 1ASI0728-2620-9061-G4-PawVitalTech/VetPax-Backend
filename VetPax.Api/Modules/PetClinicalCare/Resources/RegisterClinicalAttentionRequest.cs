namespace VetPax.Api.Modules.PetClinicalCare.Resources;

public sealed record RegisterClinicalAttentionRequest(
    DateTime AttentionDate,
    string Reason,
    string Diagnosis,
    string TreatmentSummary,
    string? Observations,
    IReadOnlyList<ClinicalIndicatorRequest>? Indicators
);

public sealed record ClinicalIndicatorRequest(
    string Name,
    decimal Value,
    string Unit
);