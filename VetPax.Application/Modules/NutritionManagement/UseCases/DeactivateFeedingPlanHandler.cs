using VetPax.Application.Modules.NutritionManagement.Models;
using VetPax.Application.Modules.NutritionManagement.Ports;
using VetPax.Application.Modules.NutritionManagement.Services;
using VetPax.Domain.Modules.NutritionManagement.Repositories;
using VetPax.Domain.Modules.NutritionManagement.ValueObjects;

namespace VetPax.Application.Modules.NutritionManagement.UseCases;

public sealed class DeactivateFeedingPlanHandler
{
    private readonly IFeedingPlanRepository _repository;
    private readonly NutritionAuthorizationService _authorization;
    private readonly INutritionReminderPort _reminders;
    private readonly NutritionCommitService _commit;
    private readonly TimeProvider _clock;

    public DeactivateFeedingPlanHandler(
        IFeedingPlanRepository repository,
        NutritionAuthorizationService authorization,
        INutritionReminderPort reminders,
        NutritionCommitService commit,
        TimeProvider clock)
    {
        _repository = repository;
        _authorization = authorization;
        _reminders = reminders;
        _commit = commit;
        _clock = clock;
    }

    public async Task<FeedingPlanData> HandleAsync(
        DeactivateFeedingPlanCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var plan = await _repository.FindByIdAsync(
            new FeedingPlanId(command.PlanId),
            cancellationToken);

        if (plan is null)
            throw new KeyNotFoundException("Feeding plan not found.");

        await _authorization.RequirePlanVeterinarianAsync(
            plan, cancellationToken);

        plan.Deactivate(_clock.GetUtcNow().UtcDateTime);

        await _reminders.CancelPendingForPlanAsync(
            plan.PlanId.Value,
            cancellationToken);

        await _commit.CommitAsync(plan, cancellationToken);

        return FeedingPlanData.From(plan);
    }
}