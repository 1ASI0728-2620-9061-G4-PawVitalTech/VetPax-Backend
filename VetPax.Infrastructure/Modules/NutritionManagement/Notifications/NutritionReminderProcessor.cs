
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VetPax.Application.Modules.NutritionManagement.Ports;
using VetPax.Domain.Modules.NutritionManagement.Repositories;
using VetPax.Domain.Modules.NutritionManagement.ValueObjects;
using VetPax.Infrastructure.Modules.NutritionManagement.Persistence;
using VetPax.Infrastructure.Persistence;

namespace VetPax.Infrastructure.Modules.NutritionManagement.Notifications;

public sealed class NutritionReminderProcessor
{
    private readonly VetPaxDbContext _db;
    private readonly IFeedingPlanRepository _plans;
    private readonly INutritionPatientPort _patients;
    private readonly INutritionPushGateway _push;
    private readonly TimeProvider _clock;
    private readonly ILogger<NutritionReminderProcessor> _logger;

    public NutritionReminderProcessor(
        VetPaxDbContext db,
        IFeedingPlanRepository plans,
        INutritionPatientPort patients,
        INutritionPushGateway push,
        TimeProvider clock,
        ILogger<NutritionReminderProcessor> logger)
    {
        _db = db;
        _plans = plans;
        _patients = patients;
        _push = push;
        _clock = clock;
        _logger = logger;
    }

    public async Task<int> ProcessBatchAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        if (batchSize is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(batchSize));

        var now = _clock.GetUtcNow().UtcDateTime;

        // Recover exhausted leases without sending again.
        await _db.Set<FeedingReminderRow>()
            .Where(x =>
                x.Status == "Processing" &&
                x.AttemptCount >=
                    NutritionReminderRetryPolicy.MaxAttempts &&
                (x.LockedUntilUtc == null ||
                 x.LockedUntilUtc <= now))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, "Failed")
                .SetProperty(x => x.LeaseId, (Guid?)null)
                .SetProperty(x => x.LockedUntilUtc, (DateTime?)null)
                .SetProperty(x => x.ProcessedAtUtc, (DateTime?)now)
                .SetProperty(x => x.LastError, "Worker lease expired"),
                cancellationToken);

        var ids = await Eligible(now)
            .AsNoTracking()
            .OrderBy(x => x.ScheduledAtUtc)
            .ThenBy(x => x.ReminderId)
            .Select(x => x.ReminderId)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        var processed = 0;

        foreach (var reminderId in ids)
        {
            cancellationToken.ThrowIfCancellationRequested();

            now = _clock.GetUtcNow().UtcDateTime;

            var leaseId = Guid.NewGuid();
            var lockedUntil = now.AddMinutes(10);

            // Atomic compare-and-set.
            // Only one worker can claim the same row.
            var claimed = await Eligible(now)
                .Where(x => x.ReminderId == reminderId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.Status, "Processing")
                    .SetProperty(x => x.LeaseId, (Guid?)leaseId)
                    .SetProperty(x => x.LockedUntilUtc,
                        (DateTime?)lockedUntil)
                    .SetProperty(x => x.NextAttemptAtUtc,
                        (DateTime?)null)
                    .SetProperty(x => x.AttemptCount,
                        x => x.AttemptCount + 1),
                    cancellationToken);

            if (claimed != 1)
                continue;

            var reminder = await _db.Set<FeedingReminderRow>()
                .AsNoTracking()
                .SingleAsync(
                    x => x.ReminderId == reminderId,
                    cancellationToken);

            var plan = await _plans.FindByIdAsync(
                new FeedingPlanId(reminder.PlanId),
                cancellationToken);

            if (plan is null ||
                plan.Version != reminder.PlanVersion ||
                !plan.CanGenerateReminder(
                    reminder.ScheduleId,
                    reminder.LocalDate))
            {
                await CompleteAsync(
                    reminderId,
                    leaseId,
                    "Cancelled",
                    null,
                    null,
                    cancellationToken);

                processed++;
                continue;
            }

            var ownerId = await _patients.GetOwnerIdAsync(
                reminder.PetId,
                cancellationToken);

            if (!ownerId.HasValue ||
                ownerId.Value == Guid.Empty)
            {
                await CompleteAsync(
                    reminderId,
                    leaseId,
                    "Cancelled",
                    null,
                    null,
                    cancellationToken);

                processed++;
                continue;
            }

            // Avoid sending very old meal reminders
            // after a lengthy service outage.
            if (now - reminder.ScheduledAtUtc >
                TimeSpan.FromHours(2))
            {
                await CompleteAsync(
                    reminderId,
                    leaseId,
                    "Cancelled",
                    null,
                    "Reminder expired",
                    cancellationToken);

                processed++;
                continue;
            }

            try
            {
                
                using var deliveryTimeout =
                    CancellationTokenSource.CreateLinkedTokenSource(
                        cancellationToken);

                deliveryTimeout.CancelAfter(TimeSpan.FromMinutes(3));

                await _push.SendAsync(
                    ownerId.Value,
                    reminder.PlanId,
                    reminder.ReminderId,
                    deliveryTimeout.Token);

            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                // Lease recovery handles interrupted work.
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    "Nutrition reminder {ReminderId} failed: {ErrorType}",
                    reminderId,
                    ex.GetType().Name);

                var failedAt = _clock.GetUtcNow().UtcDateTime;

                var next = NutritionReminderRetryPolicy.NextAttempt(
                    reminder.AttemptCount,
                    failedAt);

                await CompleteAsync(
                    reminderId,
                    leaseId,
                    next.HasValue ? "Retry" : "Failed",
                    next,
                    "FCM delivery unavailable",
                    cancellationToken);

                processed++;
                continue;
            }

            await CompleteAsync(
                reminderId,
                leaseId,
                "Sent",
                null,
                null,
                cancellationToken);

            processed++;
        }

        return processed;
    }

    private IQueryable<FeedingReminderRow> Eligible(DateTime now)
    {
        return _db.Set<FeedingReminderRow>()
            .Where(x =>
                x.ScheduledAtUtc <= now &&
                x.AttemptCount <
                    NutritionReminderRetryPolicy.MaxAttempts &&
                (
                    x.Status == "Pending" ||
                    (
                        x.Status == "Retry" &&
                        (x.NextAttemptAtUtc == null ||
                         x.NextAttemptAtUtc <= now)
                    ) ||
                    (
                        x.Status == "Processing" &&
                        (x.LockedUntilUtc == null ||
                         x.LockedUntilUtc <= now)
                    )
                ));
    }

    private async Task CompleteAsync(
        Guid reminderId,
        Guid leaseId,
        string status,
        DateTime? nextAttempt,
        string? error,
        CancellationToken cancellationToken)
    {
        var now = _clock.GetUtcNow().UtcDateTime;

        DateTime? processedAt =
            status == "Retry" ? null : now;

        await _db.Set<FeedingReminderRow>()
            .Where(x =>
                x.ReminderId == reminderId &&
                x.Status == "Processing" &&
                x.LeaseId == leaseId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, status)
                .SetProperty(x => x.NextAttemptAtUtc, nextAttempt)
                .SetProperty(x => x.ProcessedAtUtc, processedAt)
                .SetProperty(x => x.LeaseId, (Guid?)null)
                .SetProperty(x => x.LockedUntilUtc, (DateTime?)null)
                .SetProperty(x => x.LastError, error),
                cancellationToken);
    }
}
