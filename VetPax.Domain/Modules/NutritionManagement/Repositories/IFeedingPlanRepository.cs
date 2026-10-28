using VetPax.Domain.Modules.NutritionManagement.Aggregates;
using VetPax.Domain.Modules.NutritionManagement.ValueObjects;

namespace VetPax.Domain.Modules.NutritionManagement.Repositories;

public interface IFeedingPlanRepository
{
    Task<FeedingPlan?> FindByIdAsync(
        FeedingPlanId planId,
        CancellationToken cancellationToken = default);

    Task<FeedingPlan?> FindActiveByPetIdAsync(
        Guid petId,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        FeedingPlan plan,
        CancellationToken cancellationToken = default);
}