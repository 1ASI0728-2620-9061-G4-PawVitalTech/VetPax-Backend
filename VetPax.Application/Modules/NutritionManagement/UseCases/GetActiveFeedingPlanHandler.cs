using VetPax.Application.Modules.NutritionManagement.Models;
using VetPax.Application.Modules.NutritionManagement.Services;
using VetPax.Domain.Modules.NutritionManagement.Repositories;

namespace VetPax.Application.Modules.NutritionManagement.UseCases;

public sealed class GetActiveFeedingPlanHandler
{
    private readonly IFeedingPlanRepository _repository;
    private readonly NutritionAuthorizationService _authorization;

    public GetActiveFeedingPlanHandler(
        IFeedingPlanRepository repository,
        NutritionAuthorizationService authorization)
    {
        _repository = repository;
        _authorization = authorization;
    }

    public async Task<FeedingPlanData> HandleAsync(
        GetActiveFeedingPlanQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.PetId == Guid.Empty)
            throw new ArgumentException("A valid pet ID is required.");

        var plan = await _repository.FindActiveByPetIdAsync(
            query.PetId,
            cancellationToken);

        if (plan is null)
            throw new KeyNotFoundException(
                "No active feeding plan was found.");

        await _authorization.RequireReadAccessAsync(
            plan, cancellationToken);

        return FeedingPlanData.From(plan);
    }
}