
using VetPax.Application.Modules.MedicationTreatment.Exceptions;
using VetPax.Application.Modules.MedicationTreatment.Models;
using VetPax.Application.Modules.MedicationTreatment.Ports;
using VetPax.Application.Modules.MedicationTreatment.Services;
using VetPax.Domain.Modules.MedicationTreatment.Repositories;
using VetPax.Domain.Modules.MedicationTreatment.ValueObjects;

namespace VetPax.Application.Modules.MedicationTreatment.UseCases.UpdateMedicationPlan;

public sealed class UpdateMedicationPlanHandler
{
    private readonly IMedicationPlanRepository _repository;
    private readonly MedicationAuthorizationService _authorization;
    private readonly IMedicationReminderPort _reminders;
    private readonly MedicationCommitService _commit;
    private readonly TimeProvider _clock;

    public UpdateMedicationPlanHandler(
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
        UpdateMedicationPlanCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var plan = await _repository.FindByIdAsync(
            new MedicationPlanId(command.PlanId),
            cancellationToken);

        if (plan is null)
            throw new KeyNotFoundException("Medication plan not found.");

        await _authorization.RequirePlanPrescriberAsync(
            plan,
            cancellationToken);

        var now = _clock.GetUtcNow().UtcDateTime;

        switch (command.Operation)
        {
            case MedicationUpdateOperation.ChangeSchedules:
            {
                if (command.Schedules is null)
                {
                    throw new MedicationValidationException(
                        "Schedules are required for this operation.");
                }

                var schedules = MedicationScheduleFactory.Create(
                    command.Schedules);

                plan.ChangeSchedules(schedules, now);
                break;
            }

            case MedicationUpdateOperation.Complete:
                plan.Complete(now);
                break;

            case MedicationUpdateOperation.Cancel:
                plan.Cancel(now);
                break;

            default:
                throw new MedicationValidationException(
                    "Unsupported medication plan operation.");
        }

        await _reminders.CancelPendingForPlanAsync(
            plan.PlanId.Value,
            cancellationToken);

        await _commit.SaveAsync(
            plan,
            cancellationToken);

        return MedicationPlanData.From(plan);
    }
}
