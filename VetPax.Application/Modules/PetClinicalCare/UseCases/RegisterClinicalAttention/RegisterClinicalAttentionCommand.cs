using VetPax.Domain.Modules.PetClinicalCare.ValueObjects;

namespace VetPax.Application.Modules.PetClinicalCare.UseCases.RegisterClinicalAttention;

public sealed record RegisterClinicalAttentionCommand(
    PetId PetId,
    AccessRequester Requester,
    DateTime AttentionDate,
    string Reason,
    string Diagnosis,
    string TreatmentSummary,
    string? Observations,
    IReadOnlyList<ClinicalIndicatorInput>? Indicators
);

public sealed record ClinicalIndicatorInput(
    string Name,
    decimal Value,
    string Unit
);