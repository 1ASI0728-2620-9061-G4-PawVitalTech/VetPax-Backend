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
    
    public ClinicalEvolution BuildEvolutionForClinic(
        ClinicalRecord record,
        ClinicId clinicId)
    {
        ArgumentNullException.ThrowIfNull(record);

        if (clinicId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "A valid clinic ID is required.");
        }

        var entries = record
            .GetAttentionsChronologically()

            // Solo atenciones de esta clínica.
            .Where(attention =>
                attention.ClinicId.Value == clinicId.Value)

            // Extraer los indicadores de cada atención.
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

            .OrderBy(entry => entry.AttentionDate)
            .ToArray();

        return new ClinicalEvolution(
            record.PetId,
            entries
        );
    }

}