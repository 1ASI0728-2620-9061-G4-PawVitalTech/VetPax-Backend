
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VetPax.Application.Common.Ports;
using VetPax.Domain.Modules.AdherenceGamification.Events;
using VetPax.Domain.Shared.Events;
using VetPax.Infrastructure.Modules.AdherenceGamification.Persistence;
using VetPax.Infrastructure.Persistence;

namespace VetPax.Infrastructure.Modules.AdherenceGamification.Processing;

public sealed class AdherenceOutboxProcessor
{
    private readonly VetPaxDbContext _db;
    private readonly IDomainEventPublisher _publisher;
    private readonly TimeProvider _clock;
    private readonly ILogger<AdherenceOutboxProcessor> _logger;

    public AdherenceOutboxProcessor(
        VetPaxDbContext db,
        IDomainEventPublisher publisher,
        TimeProvider clock,
        ILogger<AdherenceOutboxProcessor> logger)
    {
        _db = db;
        _publisher = publisher;
        _clock = clock;
        _logger = logger;
    }

    public async Task<int> ProcessBatchAsync(
        int batchSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (batchSize is < 1 or > 100)
            throw new ArgumentOutOfRangeException(
                nameof(batchSize));

        var now = _clock.GetUtcNow().UtcDateTime;

        var eventIds = await Eligible(now)
            .AsNoTracking()
            .OrderBy(x => x.OccurredAtUtc)
            .ThenBy(x => x.EventId)
            .Select(x => x.EventId)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        var published = 0;

        foreach (var eventId in eventIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var claimedAt = _clock.GetUtcNow().UtcDateTime;
            var leaseToken = Guid.NewGuid();
            var leaseUntil = claimedAt.AddMinutes(5);

            // Atomic claim: only one worker wins.
            var claimed = await Eligible(claimedAt)
                .Where(x => x.EventId == eventId)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(
                            x => x.LeaseToken,
                            (Guid?)leaseToken)
                        .SetProperty(
                            x => x.LockedUntilUtc,
                            (DateTime?)leaseUntil)
                        .SetProperty(
                            x => x.AttemptCount,
                            x => x.AttemptCount + 1),
                    cancellationToken);

            if (claimed != 1)
                continue;

            try
            {
                var row = await _db.Set<AdherenceOutboxRow>()
                    .AsNoTracking()
                    .SingleAsync(
                        x => x.EventId == eventId,
                        cancellationToken);

                var domainEvent = Deserialize(row);

                await _publisher.PublishAsync(
                    new IDomainEvent[] { domainEvent },
                    cancellationToken);

                var completedAt = _clock
                    .GetUtcNow().UtcDateTime;

                var completed = await _db
                    .Set<AdherenceOutboxRow>()
                    .Where(x =>
                        x.EventId == eventId &&
                        x.PublishedAtUtc == null &&
                        x.LeaseToken == leaseToken)
                    .ExecuteUpdateAsync(
                        setters => setters
                            .SetProperty(
                                x => x.PublishedAtUtc,
                                (DateTime?)completedAt)
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
                    published++;
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Adherence outbox event {EventId} failed.",
                    eventId);

                var failedAt = _clock
                    .GetUtcNow().UtcDateTime;

                var attempts = await _db
                    .Set<AdherenceOutboxRow>()
                    .AsNoTracking()
                    .Where(x => x.EventId == eventId)
                    .Select(x => x.AttemptCount)
                    .SingleAsync(cancellationToken);

                var delayMinutes = Math.Min(
                    60,
                    1 << Math.Min(
                        Math.Max(attempts - 1, 0),
                        6));

                var retryAt = failedAt.AddMinutes(
                    delayMinutes);

                var error = ex.Message;

                if (error.Length > 1024)
                    error = error[..1024];

                await _db.Set<AdherenceOutboxRow>()
                    .Where(x =>
                        x.EventId == eventId &&
                        x.PublishedAtUtc == null &&
                        x.LeaseToken == leaseToken)
                    .ExecuteUpdateAsync(
                        setters => setters
                            .SetProperty(
                                x => x.NextAttemptAtUtc,
                                (DateTime?)retryAt)
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

        return published;
    }

    private IQueryable<AdherenceOutboxRow> Eligible(
        DateTime now)
    {
        return _db.Set<AdherenceOutboxRow>()
            .Where(x =>
                x.PublishedAtUtc == null &&
                (
                    x.NextAttemptAtUtc == null ||
                    x.NextAttemptAtUtc <= now
                ) &&
                (
                    x.LockedUntilUtc == null ||
                    x.LockedUntilUtc <= now
                ));
    }

    private static IDomainEvent Deserialize(
        AdherenceOutboxRow row)
    {
        var types = new Type[]
        {
            typeof(AdherenceCalculatedEvent),
            typeof(ConstancyLevelChangedEvent),
            typeof(OwnerPromotedEvent),
            typeof(RecognitionSentEvent)
        };

        var type = types.SingleOrDefault(
            x => x.FullName == row.EventType);

        if (type is null)
        {
            throw new JsonException(
                $"Unsupported adherence event: {row.EventType}");
        }

        var instance = JsonSerializer.Deserialize(
            row.PayloadJson,
            type,
            new JsonSerializerOptions(
                JsonSerializerDefaults.Web));

        return instance as IDomainEvent
            ?? throw new JsonException(
                "Invalid adherence outbox payload.");
    }
}
