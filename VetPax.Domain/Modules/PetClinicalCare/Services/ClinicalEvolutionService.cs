using VetPax.Domain.Modules.PetClinicalCare.Aggregates;
using VetPax.Domain.Modules.PetClinicalCare.ValueObjects;

namespace VetPax.Domain.Modules.PetClinicalCare.Services;

public sealed class ClinicalEvolutionService
{
    public ClinicalEvolution BuildEvolution(
        ClinicalRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        var entries = record
            .GetAttentionsChronologically()
            .SelectMany(attention =>
                attention.Indicators.Select(indicator =>
                    new EvolutionEntry(
                        attention.AttentionDate,
                        indicator.Name,
                        indicator.Value,
                        indicator.Unit
                    )
                )
            )
            .ToArray();

        var comparableEntries = entries
            .GroupBy(e => (
                e.IndicatorName.ToUpperInvariant(),
                e.Unit.ToUpperInvariant()
            ))
            .Where(group =>
                group.Select(e => e.AttentionDate)
                    .Distinct()
                    .Count() >= 2)
            .SelectMany(group => group)
            .OrderBy(e => e.AttentionDate)
            .ToArray();

        return new ClinicalEvolution(
            record.PetId,
            comparableEntries
        );
    }
}