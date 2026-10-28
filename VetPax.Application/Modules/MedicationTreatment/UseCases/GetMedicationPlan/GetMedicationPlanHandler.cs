using VetPax.Application.Modules.MedicationTreatment.Models;
using VetPax.Application.Modules.MedicationTreatment.Services;
using VetPax.Domain.Modules.MedicationTreatment.Repositories;
using VetPax.Domain.Modules.MedicationTreatment.ValueObjects;

namespace VetPax.Application.Modules.MedicationTreatment.UseCases.GetMedicationPlan;

public sealed class GetMedicationPlanHandler
{
    private readonly IMedicationPlanRepository _repository;
    private readonly MedicationAuthorizationService _authorization;

    public GetMedicationPlanHandler(
        IMedicationPlanRepository repository,
        MedicationAuthorizationService authorization)
    {
        _repository = repository;
        _authorization = authorization;
    }

    public async Task<MedicationPlanData> HandleAsync(
        GetMedicationPlanQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var plan = await _repository.FindByIdAsync(
            new MedicationPlanId(query.PlanId),
            cancellationToken);

        if (plan is null)
            throw new KeyNotFoundException("Medication plan not found.");

        await _authorization.EnsureCanReadAsync(
            plan,
            cancellationToken);

        return MedicationPlanData.From(plan);
    }
}