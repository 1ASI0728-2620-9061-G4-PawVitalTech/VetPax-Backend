using VetPax.Application.Modules.NutritionManagement.Models;
using VetPax.Application.Modules.NutritionManagement.Services;
using VetPax.Domain.Modules.NutritionManagement.Repositories;
using VetPax.Domain.Modules.NutritionManagement.ValueObjects;

namespace VetPax.Application.Modules.NutritionManagement.UseCases;

public sealed class GetFeedingPlanHandler
{
    private readonly IFeedingPlanRepository _repository;
    private readonly NutritionAuthorizationService _authorization;

    public GetFeedingPlanHandler(
        IFeedingPlanRepository repository,
        NutritionAuthorizationService authorization)
    {
        _repository = repository;
        _authorization = authorization;
    }

    public async Task<FeedingPlanData> HandleAsync(
        GetFeedingPlanQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var plan = await _repository.FindByIdAsync(
            new FeedingPlanId(query.PlanId),
            cancellationToken);

        if (plan is null)
            throw new KeyNotFoundException("Feeding plan not found.");

        await _authorization.RequireReadAccessAsync(
            plan, cancellationToken);

        return FeedingPlanData.From(plan);
    }
}