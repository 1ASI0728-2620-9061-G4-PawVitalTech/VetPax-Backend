
using VetPax.Application.Modules.NutritionManagement.Models;
using VetPax.Application.Modules.NutritionManagement.Ports;
using VetPax.Application.Modules.NutritionManagement.Services;

namespace VetPax.Application.Modules.NutritionManagement.UseCases;

public sealed class GenerateFeedingRemindersHandler
{
    private readonly INutritionReminderCandidatesPort _candidates;
    private readonly INutritionReminderPort _reminders;
    private readonly NutritionTimeService _timeService;
    private readonly NutritionCommitService _commit;
    private readonly TimeProvider _clock;

    public GenerateFeedingRemindersHandler(
        INutritionReminderCandidatesPort candidates,
        INutritionReminderPort reminders,
        NutritionTimeService timeService,
        NutritionCommitService commit,
        TimeProvider clock)
    {
        _candidates = candidates;
        _reminders = reminders;
        _timeService = timeService;
        _commit = commit;
        _clock = clock;
    }

    public async Task<int> HandleAsync(
        GenerateFeedingRemindersCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.FromDate == default ||
            command.ThroughDate < command.FromDate ||
            command.ThroughDate.DayNumber -
                command.FromDate.DayNumber > 7)
        {
            throw new ArgumentException(
                "The reminder generation range must be between 0 and 7 days.");
        }

        if (command.BatchSize is < 1 or > 100)
            throw new ArgumentOutOfRangeException(
                nameof(command.BatchSize));

        var plans = await _candidates.FindCandidatesAsync(
            command.BatchSize,
            cancellationToken);

        var generated = 0;

        foreach (var plan in plans)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!plan.RemindersEnabled)
                continue;

            var planGenerated = 0;

            for (var date = command.FromDate;
                 date <= command.ThroughDate;
                 date = date.AddDays(1))
            {
                foreach (var schedule in plan.Schedules)
                {
                    if (!plan.CanGenerateReminder(
                            schedule.ScheduleId,
                            date))
                    {
                        continue;
                    }

                    var scheduledAtUtc =
                        _timeService.TryConvertToUtc(
                            date,
                            schedule.LocalTime,
                            plan.TimeZoneId);

                    if (!scheduledAtUtc.HasValue)
                        continue;

                    var occurrence = new FeedingReminderOccurrence(
                        plan.PlanId.Value,
                        plan.PetId,
                        schedule.ScheduleId,
                        plan.Version,
                        date,
                        scheduledAtUtc.Value);

                    // Infrastructure must guarantee this
                    // operation is idempotent, including
                    // concurrent workers.
                    var staged = await _reminders.TryStageAsync(
                        occurrence,
                        cancellationToken);

                    if (!staged)
                        continue;

                    plan.RecordReminderGenerated(
                        schedule.ScheduleId,
                        date,
                        _clock.GetUtcNow().UtcDateTime);

                    planGenerated++;
                }
            }

            if (planGenerated == 0)
                continue;

            // Reminder rows and domain-event outbox rows
            // must commit in the same database operation.
            await _commit.CommitAsync(
                plan,
                cancellationToken);

            generated += planGenerated;
        }

        return generated;
    }
}
