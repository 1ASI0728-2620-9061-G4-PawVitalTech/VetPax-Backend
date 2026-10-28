
using Microsoft.EntityFrameworkCore;
using VetPax.Application.Modules.MedicationTreatment.Ports;
using VetPax.Domain.Modules.MedicationTreatment.Aggregates;
using VetPax.Domain.Modules.MedicationTreatment.Entities;
using VetPax.Infrastructure.Modules.MedicationTreatment.Persistence;
using VetPax.Infrastructure.Persistence;

namespace VetPax.Infrastructure.Modules.MedicationTreatment.Adapters;

public sealed class MedicationReminderAdapter
    : IMedicationReminderPort
{
    private readonly VetPaxDbContext _db;

    public MedicationReminderAdapter(VetPaxDbContext db)
    {
        _db = db;
    }

    public async Task StageForDoseAsync(
        MedicationPlan plan,
        MedicationDose dose,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(dose);

        if (!plan.RemindersEnabled)
            return;

        if (plan.PlanId != dose.PlanId)
        {
            throw new InvalidOperationException(
                "Dose belongs to another medication plan.");
        }

        var reminders = _db.Set<MedicationReminderRow>();

        var existing = reminders.Local.FirstOrDefault(
            x => x.DoseId == dose.DoseId.Value);

        existing ??= await reminders.SingleOrDefaultAsync(
            x => x.DoseId == dose.DoseId.Value,
            cancellationToken);

        if (existing is not null)
        {
            // No reactivar un aviso cancelado o enviado.
            return;
        }

        var now = DateTime.UtcNow;

        reminders.Add(new MedicationReminderRow
        {
            ReminderId = Guid.NewGuid(),
            PlanId = plan.PlanId.Value,
            DoseId = dose.DoseId.Value,
            OwnerId = plan.OwnerId.Value,
            DueAtUtc = dose.ScheduledAtUtc,
            NextAttemptAtUtc = dose.ScheduledAtUtc,
            Status = MedicationReminderDeliveryStatus.Pending,
            Attempts = 0,
            UpdatedAtUtc = now
        });
    }

    public async Task CancelForDoseAsync(
        Guid doseId,
        CancellationToken cancellationToken = default)
    {
        var reminders = _db.Set<MedicationReminderRow>();

        var local = reminders.Local
            .Where(x =>
                x.DoseId == doseId &&
                x.Status == MedicationReminderDeliveryStatus.Pending)
            .ToArray();

        foreach (var reminder in local)
        {
            reminder.Status =
                MedicationReminderDeliveryStatus.Cancelled;

            reminder.UpdatedAtUtc = DateTime.UtcNow;
        }

        var persisted = await reminders
            .Where(x =>
                x.DoseId == doseId &&
                x.Status == MedicationReminderDeliveryStatus.Pending)
            .ToListAsync(cancellationToken);

        foreach (var reminder in persisted)
        {
            reminder.Status =
                MedicationReminderDeliveryStatus.Cancelled;

            reminder.UpdatedAtUtc = DateTime.UtcNow;
        }
    }

    public async Task CancelPendingForPlanAsync(
        Guid planId,
        CancellationToken cancellationToken = default)
    {
        var reminders = _db.Set<MedicationReminderRow>();

        var local = reminders.Local
            .Where(x =>
                x.PlanId == planId &&
                x.Status == MedicationReminderDeliveryStatus.Pending)
            .ToArray();

        foreach (var reminder in local)
        {
            reminder.Status =
                MedicationReminderDeliveryStatus.Cancelled;

            reminder.UpdatedAtUtc = DateTime.UtcNow;
        }

        var persisted = await reminders
            .Where(x =>
                x.PlanId == planId &&
                x.Status == MedicationReminderDeliveryStatus.Pending)
            .ToListAsync(cancellationToken);

        foreach (var reminder in persisted)
        {
            reminder.Status =
                MedicationReminderDeliveryStatus.Cancelled;

            reminder.UpdatedAtUtc = DateTime.UtcNow;
        }
    }
}
