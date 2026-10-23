namespace VetPax.Domain.Modules.PetClinicalCare.ValueObjects;

public sealed class ClinicalEvolution
{
    public PetId PetId { get; }

    public IReadOnlyList<EvolutionEntry> Entries { get; }

    public ClinicalEvolution(
        PetId petId,
        IEnumerable<EvolutionEntry> entries)
    {
        if (petId.Value == Guid.Empty)
            throw new ArgumentException(
                "A valid pet ID is required.");

        ArgumentNullException.ThrowIfNull(entries);

        PetId = petId;

        Entries = Array.AsReadOnly(
            entries
                .OrderBy(e => e.AttentionDate)
                .ToArray()
        );
    }

    public bool HasSufficientData()
    {
        return Entries
            .GroupBy(e => (
                e.IndicatorName.ToUpperInvariant(),
                e.Unit.ToUpperInvariant()
            ))
            .Any(group =>
                group.Select(e => e.AttentionDate)
                    .Distinct()
                    .Count() >= 2);
    }
}