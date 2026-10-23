namespace VetPax.Api.Modules.PetClinicalCare.Resources;

public sealed record ClinicalEvolutionResponse(
    Guid PetId,
    bool HasSufficientData,
    string? Message,
    IReadOnlyList<EvolutionEntryResponse> Entries
);

public sealed record EvolutionEntryResponse(
    DateTime AttentionDate,
    string IndicatorName,
    decimal Value,
    string Unit
);