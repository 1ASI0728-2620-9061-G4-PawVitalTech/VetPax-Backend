
using VetPax.Application.Modules.MedicationTreatment.Models;
using VetPax.Application.Modules.MedicationTreatment.Ports;
using VetPax.Application.Modules.MedicationTreatment.Services;
using VetPax.Domain.Modules.MedicationTreatment.Enums;
using VetPax.Domain.Modules.MedicationTreatment.Repositories;
using VetPax.Domain.Modules.MedicationTreatment.ValueObjects;

namespace VetPax.Application.Modules.MedicationTreatment.UseCases.ConfigureMedicationReminders;

public sealed class ConfigureMedicationRemindersHandler
{
    private readonly IMedicationPlanRepository _repository;
    private readonly MedicationAuthorizationService _authorization;
    private readonly IMedicationReminderPort _reminders;
    private readonly MedicationCommitService _commit;
    private readonly TimeProvider _clock;

    public ConfigureMedicationRemindersHandler(
        IMedicationPlanRepository repository,
        MedicationAuthorizationService authorization,
        IMedicationReminderPort reminders,
        MedicationCommitService commit,
        TimeProvider clock)
    {
        _repository = repository;
        _authorization = authorization;
        _reminders = reminders;
        _commit = commit;
        _clock = clock;
    }

    public async Task<MedicationPlanData> HandleAsync(
        ConfigureMedicationRemindersCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var plan = await _repository.FindByIdAsync(
            new MedicationPlanId(command.PlanId),
            cancellationToken);

        if (plan is null)
            throw new KeyNotFoundException("Medication plan not found.");

        await _authorization.RequirePlanOwnerAsync(
            plan,
            cancellationToken);

        var now = _clock.GetUtcNow().UtcDateTime;

        var wasEnabled = plan.RemindersEnabled;

        plan.ConfigureReminders(
            command.Enabled,
            now);

        if (wasEnabled == command.Enabled)
        {
            return MedicationPlanData.From(plan);
        }

        if (command.Enabled)
        {
            foreach (var dose in plan.Doses)
            {
                if (dose.Status != MedicationDoseStatus.Pending ||
                    dose.ScheduleVersion != plan.ScheduleVersion ||
                    dose.ScheduledAtUtc <= now)
                {
                    continue;
                }

                await _reminders.StageForDoseAsync(
                    plan,
                    dose,
                    cancellationToken);
            }
        }
        else
        {
            await _reminders.CancelPendingForPlanAsync(
                plan.PlanId.Value,
                cancellationToken);
        }

        await _commit.SaveAsync(
            plan,
            cancellationToken);

        return MedicationPlanData.From(plan);
    }
}
