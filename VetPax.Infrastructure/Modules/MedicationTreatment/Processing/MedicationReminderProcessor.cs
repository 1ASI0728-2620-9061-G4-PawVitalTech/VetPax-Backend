
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VetPax.Application.Modules.MedicationTreatment.Models;
using VetPax.Application.Modules.MedicationTreatment.Ports;
using VetPax.Domain.Modules.MedicationTreatment.Enums;
using VetPax.Infrastructure.Modules.MedicationTreatment.Persistence;
using VetPax.Infrastructure.Persistence;

namespace VetPax.Infrastructure.Modules.MedicationTreatment.Processing;

public sealed class MedicationReminderProcessor
{
    private readonly VetPaxDbContext _db;
    private readonly IMedicationNotificationSender _sender;
    private readonly TimeProvider _clock;
    private readonly ILogger<MedicationReminderProcessor> _logger;

    public MedicationReminderProcessor(
        VetPaxDbContext db,
        IMedicationNotificationSender sender,
        TimeProvider clock,
        ILogger<MedicationReminderProcessor> logger)
    {
        _db = db;
        _sender = sender;
        _clock = clock;
        _logger = logger;
    }

    public async Task<int> ProcessBatchAsync(
        CancellationToken cancellationToken = default)
    {
        var now = _clock.GetUtcNow().UtcDateTime;

        var candidates = await _db.Set<MedicationReminderRow>()
            .AsNoTracking()
            .Where(x =>
                x.Status == MedicationReminderDeliveryStatus.Pending &&
                x.DueAtUtc <= now &&
                x.NextAttemptAtUtc <= now &&
                (x.LockedUntilUtc == null ||
                 x.LockedUntilUtc <= now))
            .OrderBy(x => x.DueAtUtc)
            .Take(20)
            .Select(x => x.ReminderId)
            .ToListAsync(cancellationToken);

        var sent = 0;

        foreach (var reminderId in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var claimedAt = _clock.GetUtcNow().UtcDateTime;
            var leaseToken = Guid.NewGuid();
            var leaseUntil = claimedAt.AddMinutes(5);

            var acquired = await _db.Set<MedicationReminderRow>()
                .Where(x =>
                    x.ReminderId == reminderId &&
                    x.Status == MedicationReminderDeliveryStatus.Pending &&
                    x.NextAttemptAtUtc <= claimedAt &&
                    (x.LockedUntilUtc == null ||
                     x.LockedUntilUtc <= claimedAt))
                .ExecuteUpdateAsync(
                    update => update
                        .SetProperty(
                            x => x.LeaseToken,
                            (Guid?)leaseToken)
                        .SetProperty(
                            x => x.LockedUntilUtc,
                            (DateTime?)leaseUntil)
                        .SetProperty(
                            x => x.Attempts,
                            x => x.Attempts + 1),
                    cancellationToken);

            if (acquired != 1)
                continue;

            try
            {
                var reminder = await _db.Set<MedicationReminderRow>()
                    .AsNoTracking()
                    .SingleAsync(
                        x => x.ReminderId == reminderId,
                        cancellationToken);

                var dose = await _db.Set<MedicationDoseRow>()
                    .AsNoTracking()
                    .SingleOrDefaultAsync(
                        x => x.DoseId == reminder.DoseId,
                        cancellationToken);

                var plan = await _db.Set<MedicationPlanRow>()
                    .AsNoTracking()
                    .SingleOrDefaultAsync(
                        x => x.PlanId == reminder.PlanId,
                        cancellationToken);

                var eligible =
                    plan is not null &&
                    dose is not null &&
                    plan.Status == MedicationPlanStatus.Active &&
                    plan.RemindersEnabled &&
                    dose.Status == MedicationDoseStatus.Pending &&
                    dose.ScheduleVersion == plan.ScheduleVersion;

                if (!eligible)
                {
                    await _db.Set<MedicationReminderRow>()
                        .Where(x =>
                            x.ReminderId == reminderId &&
                            x.LeaseToken == leaseToken &&
                            x.Status ==
                                MedicationReminderDeliveryStatus.Pending)
                        .ExecuteUpdateAsync(
                            update => update
                                .SetProperty(
                                    x => x.Status,
                                    MedicationReminderDeliveryStatus.Cancelled)
                                .SetProperty(
                                    x => x.LeaseToken,
                                    (Guid?)null)
                                .SetProperty(
                                    x => x.LockedUntilUtc,
                                    (DateTime?)null),
                            cancellationToken);

                    continue;
                }

                // Comprobación final antes de llamar
                // al proveedor externo.
                var stillEligible = await (
                    from r in _db.Set<MedicationReminderRow>()
                    join d in _db.Set<MedicationDoseRow>()
                        on r.DoseId equals d.DoseId
                    join p in _db.Set<MedicationPlanRow>()
                        on r.PlanId equals p.PlanId
                    where r.ReminderId == reminderId &&
                          r.LeaseToken == leaseToken &&
                          r.Status ==
                              MedicationReminderDeliveryStatus.Pending &&
                          d.Status == MedicationDoseStatus.Pending &&
                          d.ScheduleVersion == p.ScheduleVersion &&
                          p.Status == MedicationPlanStatus.Active &&
                          p.RemindersEnabled
                    select r.ReminderId
                ).AnyAsync(cancellationToken);

                if (!stillEligible)
                    continue;

                var message = new MedicationNotificationMessage(
                    reminder.ReminderId,
                    reminder.OwnerId,
                    reminder.PlanId,
                    reminder.DoseId,
                    dose!.ScheduledAtUtc,
                    "Recordatorio de medicación",
                    $"Es hora de revisar la medicación " +
                    $"programada para tu mascota.");

                await _sender.SendAsync(
                    message,
                    cancellationToken);

                var completedAt = _clock.GetUtcNow().UtcDateTime;

                var completed = await _db.Set<MedicationReminderRow>()
                    .Where(x =>
                        x.ReminderId == reminderId &&
                        x.LeaseToken == leaseToken &&
                        x.Status ==
                            MedicationReminderDeliveryStatus.Pending)
                    .ExecuteUpdateAsync(
                        update => update
                            .SetProperty(
                                x => x.Status,
                                MedicationReminderDeliveryStatus.Sent)
                            .SetProperty(
                                x => x.SentAtUtc,
                                (DateTime?)completedAt)
                            .SetProperty(
                                x => x.UpdatedAtUtc,
                                completedAt)
                            .SetProperty(
                                x => x.LeaseToken,
                                (Guid?)null)
                            .SetProperty(
                                x => x.LockedUntilUtc,
                                (DateTime?)null)
                            .SetProperty(
                                x => x.LastError,
                                (string?)null),
                        cancellationToken);

                if (completed == 1)
                    sent++;
            }
            catch (Exception exception)
                when (exception is not OperationCanceledException)
            {
                _logger.LogError(
                    exception,
                    "Medication reminder {ReminderId} failed.",
                    reminderId);

                var error = exception.ToString();
                if (error.Length > 1024)
                    error = error[..1024];

                var attempts = await _db.Set<MedicationReminderRow>()
                    .AsNoTracking()
                    .Where(x => x.ReminderId == reminderId)
                    .Select(x => x.Attempts)
                    .SingleAsync(cancellationToken);

                var minutes = Math.Min(
                    60,
                    1 << Math.Min(Math.Max(attempts - 1, 0), 6));

                var retryAt = _clock.GetUtcNow()
                    .UtcDateTime.AddMinutes(minutes);

                await _db.Set<MedicationReminderRow>()
                    .Where(x =>
                        x.ReminderId == reminderId &&
                        x.LeaseToken == leaseToken &&
                        x.Status ==
                            MedicationReminderDeliveryStatus.Pending)
                    .ExecuteUpdateAsync(
                        update => update
                            .SetProperty(
                                x => x.NextAttemptAtUtc,
                                retryAt)
                            .SetProperty(
                                x => x.LastError,
                                error)
                            .SetProperty(
                                x => x.LeaseToken,
                                (Guid?)null)
                            .SetProperty(
                                x => x.LockedUntilUtc,
                                (DateTime?)null),
                        cancellationToken);
            }
        }

        return sent;
    }
}
