
using VetPax.Application.Modules.MedicationTreatment.Exceptions;
using VetPax.Application.Modules.MedicationTreatment.Models;
using VetPax.Application.Modules.MedicationTreatment.Services;
using VetPax.Domain.Modules.MedicationTreatment.Repositories;
using VetPax.Domain.Modules.MedicationTreatment.ValueObjects;

namespace VetPax.Application.Modules.MedicationTreatment.UseCases.GetMedicationDoses;

public sealed class GetMedicationDosesHandler
{
    private readonly IMedicationPlanRepository _repository;
    private readonly MedicationAuthorizationService _authorization;

    public GetMedicationDosesHandler(
        IMedicationPlanRepository repository,
        MedicationAuthorizationService authorization)
    {
        _repository = repository;
        _authorization = authorization;
    }

    public async Task<IReadOnlyList<MedicationDoseData>> HandleAsync(
        GetMedicationDosesQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.From.HasValue &&
            query.To.HasValue &&
            query.From.Value > query.To.Value)
        {
            throw new MedicationValidationException(
                "The start date cannot be after the end date.");
        }

        var plan = await _repository.FindByIdAsync(
            new MedicationPlanId(query.PlanId),
            cancellationToken);

        if (plan is null)
            throw new KeyNotFoundException("Medication plan not found.");

        await _authorization.EnsureCanReadAsync(
            plan,
            cancellationToken);

        return plan.Doses
            .Where(x =>
                (!query.From.HasValue ||
                 x.LocalDate >= query.From.Value) &&
                (!query.To.HasValue ||
                 x.LocalDate <= query.To.Value))
            .OrderBy(x => x.ScheduledAtUtc)
            .ThenBy(x => x.DoseId.Value)
            .Select(MedicationDoseData.From)
            .ToArray();
    }
}
