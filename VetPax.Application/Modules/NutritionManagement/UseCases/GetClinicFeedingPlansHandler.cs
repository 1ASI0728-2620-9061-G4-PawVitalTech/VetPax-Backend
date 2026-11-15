using VetPax.Application.Modules.NutritionManagement.Models;
using VetPax.Application.Modules.NutritionManagement.Services;
using VetPax.Domain.Modules.NutritionManagement.Repositories;

namespace VetPax.Application.Modules.NutritionManagement.UseCases;

public sealed class GetClinicFeedingPlansHandler
{
    private readonly IFeedingPlanRepository _repository;
    private readonly NutritionAuthorizationService _authorization;

    public GetClinicFeedingPlansHandler(
        IFeedingPlanRepository repository,
        NutritionAuthorizationService authorization)
    {
        _repository = repository;
        _authorization = authorization;
    }

    public async Task<IReadOnlyList<FeedingPlanData>> HandleAsync(
        Guid petId,
        Guid clinicId,
        CancellationToken cancellationToken = default)
    {
        if (petId == Guid.Empty || clinicId == Guid.Empty)
        {
            throw new ArgumentException(
                "Valid pet and clinic IDs are required.");
        }

        // Verifica rol, veterinario activo y paciente
        // vinculado a la clínica.
        await _authorization.RequireVeterinarianAsync(
            petId,
            clinicId,
            cancellationToken);

        var plans = await _repository.ListByPetAndClinicAsync(
            petId,
            clinicId,
            cancellationToken);

        return plans
            .Select(FeedingPlanData.From)
            .ToArray();
    }
}