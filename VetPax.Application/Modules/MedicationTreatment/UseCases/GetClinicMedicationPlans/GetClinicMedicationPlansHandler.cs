using VetPax.Application.Modules.MedicationTreatment.Models;
using VetPax.Application.Modules.MedicationTreatment.Services;
using VetPax.Domain.Modules.MedicationTreatment.Repositories;
using VetPax.Domain.Modules.MedicationTreatment.ValueObjects;

namespace VetPax.Application.Modules.MedicationTreatment
    .UseCases.GetClinicMedicationPlans;

public sealed class GetClinicMedicationPlansHandler
{
    private readonly IMedicationPlanRepository _repository;
    private readonly MedicationAuthorizationService _authorization;

    public GetClinicMedicationPlansHandler(
        IMedicationPlanRepository repository,
        MedicationAuthorizationService authorization)
    {
        _repository = repository;
        _authorization = authorization;
    }

    public async Task<IReadOnlyList<MedicationPlanData>> HandleAsync(
        Guid petId,
        Guid clinicId,
        CancellationToken cancellationToken = default)
    {
        if (petId == Guid.Empty || clinicId == Guid.Empty)
        {
            throw new ArgumentException(
                "Valid pet and clinic IDs are required.");
        }

        // Comprueba el rol veterinario, la pertenencia
        // a la clínica y la relación activa del paciente.
        await _authorization.RequirePrescriberAsync(
            petId,
            clinicId,
            cancellationToken);

        var plans = await _repository.ListByPetAsync(
            new MedicationPetId(petId),
            cancellationToken);

        return plans
            .Where(plan => plan.ClinicId.Value == clinicId)
            .Select(MedicationPlanData.From)
            .ToArray();
    }
}