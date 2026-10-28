
using VetPax.Application.Modules.MedicationTreatment.Exceptions;
using VetPax.Application.Modules.MedicationTreatment.Models;
using VetPax.Application.Modules.MedicationTreatment.Ports;
using VetPax.Application.Modules.MedicationTreatment.Services;
using VetPax.Domain.Modules.MedicationTreatment.Enums;
using VetPax.Domain.Modules.MedicationTreatment.Repositories;

namespace VetPax.Application.Modules.MedicationTreatment.UseCases.GenerateMedicationDoses;

public sealed class GenerateMedicationDosesHandler
{
    private readonly IMedicationPlanRepository _repository;
    private readonly MedicationTimeService _time;
    private readonly IMedicationReminderPort _reminders;
    private readonly MedicationCommitService _commit;
    private readonly TimeProvider _clock;

    public GenerateMedicationDosesHandler(
        IMedicationPlanRepository repository,
        MedicationTimeService time,
        IMedicationReminderPort reminders,
        MedicationCommitService commit,
        TimeProvider clock)
    {
        _repository = repository;
        _time = time;
        _reminders = reminders;
        _commit = commit;
        _clock = clock;
    }

    public async Task<int> HandleAsync(
        GenerateMedicationDosesCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.BatchSize is < 1 or > 100)
        {
            throw new MedicationValidationException(
                "Batch size must be between 1 and 100.");
        }

        var now = _clock.GetUtcNow().UtcDateTime;
        var todayUtc = DateOnly.FromDateTime(now);

        if (command.ThroughUtcDate < todayUtc ||
            command.ThroughUtcDate > todayUtc.AddDays(7))
        {
            throw new MedicationValidationException(
                "Dose generation horizon must be within the next seven UTC days.");
        }

        var plans = await _repository.FindEligibleForGenerationAsync(
            command.ThroughUtcDate,
            command.BatchSize,
            cancellationToken);

        var totalCreated = 0;

        foreach (var plan in plans)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (plan.Status != MedicationPlanStatus.Active)
                continue;

            var localToday = _time.GetLocalDate(
                now,
                plan.TimeZoneId);

            var from = localToday > plan.StartDate
                ? localToday
                : plan.StartDate;

            var through = command.ThroughUtcDate;

            if (plan.EndDate.HasValue &&
                plan.EndDate.Value < through)
            {
                through = plan.EndDate.Value;
            }

            if (from > through)
                continue;

            var createdForPlan = 0;

            for (var date = from;
                 date <= through;
                 date = date.AddDays(1))
            {
                foreach (var schedule in plan.Schedules)
                {
                    if (!schedule.AppliesOn(date))
                        continue;

                    // Evitar generar la misma ocurrencia
                    // por segunda vez.
                    var exists = plan.Doses.Any(x =>
                        x.ScheduleId == schedule.ScheduleId &&
                        x.ScheduleVersion == plan.ScheduleVersion &&
                        x.LocalDate == date);

                    if (exists)
                        continue;

                    var scheduledAtUtc = _time.ConvertToUtc(
                        date,
                        schedule.LocalTime,
                        plan.TimeZoneId);

                    // No generamos nuevas dosis cuyo horario
                    // ya pasó al momento de procesamiento.
                    if (scheduledAtUtc <= now)
                        continue;

                    var dose = plan.ScheduleDose(
                        schedule.ScheduleId,
                        date,
                        scheduledAtUtc,
                        now);

                    createdForPlan++;

                    if (plan.RemindersEnabled)
                    {
                        await _reminders.StageForDoseAsync(
                            plan,
                            dose,
                            cancellationToken);
                    }
                }
            }

            if (createdForPlan == 0)
                continue;

            await _commit.SaveAsync(
                plan,
                cancellationToken);

            totalCreated += createdForPlan;
        }

        return totalCreated;
    }
}
