
using VetPax.Application.Modules.NutritionManagement.Models;
using VetPax.Application.Modules.NutritionManagement.Ports;
using VetPax.Application.Modules.NutritionManagement.Services;
using VetPax.Domain.Modules.NutritionManagement.Repositories;
using VetPax.Domain.Modules.NutritionManagement.ValueObjects;

namespace VetPax.Application.Modules.NutritionManagement.UseCases;

public sealed class UpdateFeedingPlanHandler
{
    private readonly IFeedingPlanRepository _repository;
    private readonly NutritionAuthorizationService _authorization;
    private readonly NutritionScheduleFactory _scheduleFactory;
    private readonly NutritionTimeService _timeService;
    private readonly INutritionReminderPort _reminders;
    private readonly NutritionCommitService _commit;
    private readonly TimeProvider _clock;

    public UpdateFeedingPlanHandler(
        IFeedingPlanRepository repository,
        NutritionAuthorizationService authorization,
        NutritionScheduleFactory scheduleFactory,
        NutritionTimeService timeService,
        INutritionReminderPort reminders,
        NutritionCommitService commit,
        TimeProvider clock)
    {
        _repository = repository;
        _authorization = authorization;
        _scheduleFactory = scheduleFactory;
        _timeService = timeService;
        _reminders = reminders;
        _commit = commit;
        _clock = clock;
    }

    public async Task<FeedingPlanData> HandleAsync(
        UpdateFeedingPlanCommand command,
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

        _timeService.ValidateTimeZone(command.TimeZoneId);

        var schedules = _scheduleFactory.Create(
            command.Schedules);

        plan.Update(
            command.FoodName,
            command.Recommendations,
            command.StartDate,
            command.EndDate,
            command.TimeZoneId,
            schedules,
            _clock.GetUtcNow().UtcDateTime);

        // Old pending reminders must not survive
        // a change in the prescribed schedules.
        await _reminders.CancelPendingForPlanAsync(
            plan.PlanId.Value,
            cancellationToken);

        await _commit.CommitAsync(plan, cancellationToken);

        return FeedingPlanData.From(plan);
    }
}
