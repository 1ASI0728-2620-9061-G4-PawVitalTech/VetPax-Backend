using VetPax.Domain.Modules.MedicationTreatment.Aggregates;
using VetPax.Domain.Modules.MedicationTreatment.ValueObjects;

namespace VetPax.Domain.Modules.MedicationTreatment.Repositories;

public interface IMedicationPlanRepository
{
    Task<MedicationPlan?> FindByIdAsync(
        MedicationPlanId planId,
        CancellationToken cancellationToken = default);

    Task<MedicationPlan?> FindByDoseIdAsync(
        MedicationDoseId doseId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MedicationPlan>> ListByPetAsync(
        MedicationPetId petId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MedicationPlan>> FindEligibleForGenerationAsync(
        DateOnly throughDate,
        int limit,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        MedicationPlan plan,
        CancellationToken cancellationToken = default);
}