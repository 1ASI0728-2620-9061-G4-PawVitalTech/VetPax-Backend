using VetPax.Domain.Modules.PetClinicalCare.ValueObjects;

namespace VetPax.Application.Modules.PetClinicalCare.UseCases.GetClinicalEvolution;

public sealed record GetClinicalEvolutionResult(
    Guid PetId,
    bool HasSufficientData,
    IReadOnlyList<EvolutionEntry> Entries
);