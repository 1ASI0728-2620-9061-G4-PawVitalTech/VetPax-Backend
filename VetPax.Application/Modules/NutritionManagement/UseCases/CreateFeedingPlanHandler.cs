
using VetPax.Application.Modules.NutritionManagement.Models;
using VetPax.Application.Modules.NutritionManagement.Services;
using VetPax.Domain.Modules.NutritionManagement.Aggregates;
using VetPax.Domain.Modules.NutritionManagement.Repositories;
using VetPax.Domain.Modules.NutritionManagement.ValueObjects;

namespace VetPax.Application.Modules.NutritionManagement.UseCases;

public sealed class CreateFeedingPlanHandler
{
    private readonly IFeedingPlanRepository _repository;
    private readonly NutritionAuthorizationService _authorization;
    private readonly NutritionScheduleFactory _scheduleFactory;
    private readonly NutritionTimeService _timeService;
    private readonly NutritionCommitService _commit;
    private readonly TimeProvider _clock;

    public CreateFeedingPlanHandler(
        IFeedingPlanRepository repository,
        NutritionAuthorizationService authorization,
        NutritionScheduleFactory scheduleFactory,
        NutritionTimeService timeService,
        NutritionCommitService commit,
        TimeProvider clock)
    {
        _repository = repository;
        _authorization = authorization;
        _scheduleFactory = scheduleFactory;
        _timeService = timeService;
        _commit = commit;
        _clock = clock;
    }

    public async Task<FeedingPlanData> HandleAsync(
        CreateFeedingPlanCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.PetId == Guid.Empty ||
            command.ClinicId == Guid.Empty)
        {
            throw new ArgumentException(
                "Valid pet and clinic IDs are required.");
        }

        var veterinarianId =
            await _authorization.RequireVeterinarianAsync(
                command.PetId,
                command.ClinicId,
                cancellationToken);

        var existing = await _repository.FindActiveByPetIdAsync(
            command.PetId, cancellationToken);

        if (existing is not null)
            throw new InvalidOperationException(
                "The pet already has an active feeding plan.");

        _timeService.ValidateTimeZone(command.TimeZoneId);

        var schedules = _scheduleFactory.Create(
            command.Schedules);

        var plan = FeedingPlan.Prescribe(
            FeedingPlanId.New(),
            command.PetId,
            command.ClinicId,
            veterinarianId,
            command.FoodName,
            command.Recommendations,
            command.StartDate,
            command.EndDate,
            command.TimeZoneId,
            schedules,
            _clock.GetUtcNow().UtcDateTime);

        await _commit.CommitAsync(plan, cancellationToken);

        return FeedingPlanData.From(plan);
    }
}
