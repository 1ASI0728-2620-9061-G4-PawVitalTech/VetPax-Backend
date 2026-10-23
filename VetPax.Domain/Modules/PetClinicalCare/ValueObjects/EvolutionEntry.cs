namespace VetPax.Domain.Modules.PetClinicalCare.ValueObjects;

public sealed record EvolutionEntry(
    DateTime AttentionDate,
    string IndicatorName,
    decimal Value,
    string Unit
);