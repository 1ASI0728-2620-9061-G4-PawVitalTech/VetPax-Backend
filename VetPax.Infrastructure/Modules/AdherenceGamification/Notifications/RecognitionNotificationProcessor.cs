
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VetPax.Domain.Modules.AdherenceGamification.Events;
using VetPax.Infrastructure.Modules.AdherenceGamification.Persistence;
using VetPax.Infrastructure.Persistence;

namespace VetPax.Infrastructure.Modules.AdherenceGamification.Notifications;

public sealed class RecognitionNotificationProcessor
{
    private const int MaxAttempts = 5;

    private readonly VetPaxDbContext _db;
    private readonly IRecognitionPushGateway _gateway;
    private readonly TimeProvider _clock;
    private readonly ILogger<RecognitionNotificationProcessor> _logger;

    public RecognitionNotificationProcessor(
        VetPaxDbContext db,
        IRecognitionPushGateway gateway,
        TimeProvider clock,
        ILogger<RecognitionNotificationProcessor> logger)
    {
        _db = db;
        _gateway = gateway;
        _clock = clock;
        _logger = logger;
    }

    public async Task<int> ProcessBatchAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        if (batchSize is < 1 or > 100)
            throw new ArgumentOutOfRangeException(
                nameof(batchSize));

        var now = _clock.GetUtcNow().UtcDateTime;

        // Recover exhausted leases.
        await _db.Set<AdherenceRecognitionRow>()
            .Where(x =>
                x.DeliveryStatus == "Processing" &&
                x.AttemptCount >= MaxAttempts &&
                x.LockedUntilUtc <= now)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.DeliveryStatus, "Failed")
                .SetProperty(x => x.LeaseId, (Guid?)null)
                .SetProperty(x => x.LockedUntilUtc, (DateTime?)null),
                cancellationToken);

        var ids = await Due(now)
            .AsNoTracking()
            .OrderBy(x => x.CreatedAtUtc)
            .ThenBy(x => x.RecognitionId)
            .Select(x => x.RecognitionId)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        var processed = 0;

        foreach (var recognitionId in ids)
        {
            cancellationToken.ThrowIfCancellationRequested();

            now = _clock.GetUtcNow().UtcDateTime;

            var leaseId = Guid.NewGuid();

            var claimed = await Due(now)
                .Where(x => x.RecognitionId == recognitionId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.DeliveryStatus, "Processing")
                    .SetProperty(x => x.LeaseId, (Guid?)leaseId)
                    .SetProperty(
                        x => x.LockedUntilUtc,
                        (DateTime?)now.AddMinutes(5))
                    .SetProperty(
                        x => x.NextAttemptAtUtc,
                        (DateTime?)null)
                    .SetProperty(
                        x => x.AttemptCount,
                        x => x.AttemptCount + 1),
                    cancellationToken);

            if (claimed != 1)
                continue;

            var recognition = await _db
                .Set<AdherenceRecognitionRow>()
                .AsNoTracking()
                .SingleAsync(
                    x => x.RecognitionId == recognitionId,
                    cancellationToken);

            string nextStatus;
            DateTime? nextAttemptAt = null;

            try
            {
                using var timeout =
                    CancellationTokenSource.CreateLinkedTokenSource(
                        cancellationToken);

                timeout.CancelAfter(TimeSpan.FromMinutes(2));

                await _gateway.SendAsync(
                    recognition.OwnerId,
                    recognition.RecognitionId,
                    recognition.ToLevel,
                    timeout.Token);

                nextStatus = "Sent";
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    "Recognition {RecognitionId} delivery failed: {Error}",
                    recognitionId,
                    ex.GetType().Name);

                nextStatus = recognition.AttemptCount >= MaxAttempts
                    ? "Failed"
                    : "Retry";

                if (nextStatus == "Retry")
                {
                    var minutes = Math.Pow(
                        2,
                        recognition.AttemptCount);

                    nextAttemptAt = _clock.GetUtcNow()
                        .UtcDateTime
                        .AddMinutes(minutes);
                }
            }

            var finishedAt = _clock.GetUtcNow().UtcDateTime;

            // Update delivery state and outgoing event
            // in the same MySQL transaction.
            await using var transaction =
                await _db.Database.BeginTransactionAsync(
                    cancellationToken);

            var updated = await _db
                .Set<AdherenceRecognitionRow>()
                .Where(x =>
                    x.RecognitionId == recognitionId &&
                    x.DeliveryStatus == "Processing" &&
                    x.LeaseId == leaseId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.DeliveryStatus, nextStatus)
                    .SetProperty(x => x.LeaseId, (Guid?)null)
                    .SetProperty(x => x.LockedUntilUtc, (DateTime?)null)
                    .SetProperty(x => x.NextAttemptAtUtc, nextAttemptAt)
                    .SetProperty(
                        x => x.SentAtUtc,
                        nextStatus == "Sent"
                            ? (DateTime?)finishedAt
                            : null),
                    cancellationToken);

            if (updated == 1 && nextStatus == "Sent")
            {
                var domainEvent = new RecognitionSentEvent(
                    recognition.OwnerId,
                    recognition.RecognitionId,
                    finishedAt);

                await _db.Set<AdherenceOutboxRow>()
                    .AddAsync(new AdherenceOutboxRow
                    {
                        EventId = Guid.NewGuid(),
                        EventType =
                            typeof(RecognitionSentEvent).FullName!,
                        PayloadJson = JsonSerializer.Serialize(
                            domainEvent),
                        OccurredAtUtc = finishedAt,
                        AttemptCount = 0
                    }, cancellationToken);

                await _db.SaveChangesAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);

            processed++;
        }

        return processed;
    }

    private IQueryable<AdherenceRecognitionRow> Due(DateTime now)
    {
        return _db.Set<AdherenceRecognitionRow>()
            .Where(x =>
                x.AttemptCount < MaxAttempts &&
                (
                    x.DeliveryStatus == "Pending" ||
                    (
                        x.DeliveryStatus == "Retry" &&
                        (x.NextAttemptAtUtc == null ||
                         x.NextAttemptAtUtc <= now)
                    ) ||
                    (
                        x.DeliveryStatus == "Processing" &&
                        x.LockedUntilUtc <= now
                    )
                ));
    }
}
