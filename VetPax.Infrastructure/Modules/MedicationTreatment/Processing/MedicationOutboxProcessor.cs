
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VetPax.Application.Common.Ports;
using VetPax.Domain.Modules.MedicationTreatment.Events;
using VetPax.Domain.Shared.Events;
using VetPax.Infrastructure.Modules.MedicationTreatment.Persistence;
using VetPax.Infrastructure.Persistence;

namespace VetPax.Infrastructure.Modules.MedicationTreatment.Processing;

public sealed class MedicationOutboxProcessor
{
    private readonly VetPaxDbContext _db;
    private readonly IDomainEventPublisher _publisher;
    private readonly TimeProvider _clock;
    private readonly ILogger<MedicationOutboxProcessor> _logger;

    public MedicationOutboxProcessor(
        VetPaxDbContext db,
        IDomainEventPublisher publisher,
        TimeProvider clock,
        ILogger<MedicationOutboxProcessor> logger)
    {
        _db = db;
        _publisher = publisher;
        _clock = clock;
        _logger = logger;
    }

    public async Task<int> ProcessBatchAsync(
        CancellationToken cancellationToken = default)
    {
        var now = _clock.GetUtcNow().UtcDateTime;

        var candidates = await _db.Set<MedicationOutboxRow>()
            .AsNoTracking()
            .Where(x =>
                x.PublishedAtUtc == null &&
                x.NextAttemptAtUtc <= now &&
                (x.LockedUntilUtc == null ||
                 x.LockedUntilUtc <= now))
            .OrderBy(x => x.OccurredAtUtc)
            .ThenBy(x => x.EventId)
            .Take(20)
            .Select(x => x.EventId)
            .ToListAsync(cancellationToken);

        var published = 0;

        foreach (var eventId in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var claimedAt = _clock.GetUtcNow().UtcDateTime;
            var leaseToken = Guid.NewGuid();
            var leaseUntil = claimedAt.AddMinutes(5);

            var acquired = await _db.Set<MedicationOutboxRow>()
                .Where(x =>
                    x.EventId == eventId &&
                    x.PublishedAtUtc == null &&
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
                var row = await _db.Set<MedicationOutboxRow>()
                    .AsNoTracking()
                    .SingleAsync(
                        x => x.EventId == eventId,
                        cancellationToken);

                var domainEvent = Deserialize(row);

                await _publisher.PublishAsync(
                    new IDomainEvent[] { domainEvent },
                    cancellationToken);

                var completedAt = _clock.GetUtcNow().UtcDateTime;

                var updated = await _db.Set<MedicationOutboxRow>()
                    .Where(x =>
                        x.EventId == eventId &&
                        x.PublishedAtUtc == null &&
                        x.LeaseToken == leaseToken)
                    .ExecuteUpdateAsync(
                        update => update
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

                if (updated == 1)
                    published++;
            }
            catch (Exception exception)
                when (exception is not OperationCanceledException)
            {
                _logger.LogError(
                    exception,
                    "Medication outbox event {EventId} failed.",
                    eventId);

                var error = exception.ToString();
                if (error.Length > 1024)
                    error = error[..1024];

                var attempts = await _db.Set<MedicationOutboxRow>()
                    .AsNoTracking()
                    .Where(x => x.EventId == eventId)
                    .Select(x => x.Attempts)
                    .SingleAsync(cancellationToken);

                var minutes = Math.Min(
                    60,
                    1 << Math.Min(Math.Max(attempts - 1, 0), 6));

                var retryAt = _clock.GetUtcNow()
                    .UtcDateTime.AddMinutes(minutes);

                await _db.Set<MedicationOutboxRow>()
                    .Where(x =>
                        x.EventId == eventId &&
                        x.PublishedAtUtc == null &&
                        x.LeaseToken == leaseToken)
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

        return published;
    }

    private static IDomainEvent Deserialize(
        MedicationOutboxRow row)
    {
        var supportedTypes = new Type[]
        {
            typeof(MedicationPlanPrescribedEvent),
            typeof(MedicationScheduleChangedEvent),
            typeof(MedicationRemindersConfiguredEvent),
            typeof(MedicationDoseScheduledEvent),
            typeof(MedicationReminderGeneratedEvent),
            typeof(MedicationDoseAdministeredEvent),
            typeof(MedicationPlanClosedEvent)
        };

        var eventType = supportedTypes.SingleOrDefault(
            x => x.FullName == row.EventType);

        if (eventType is null)
        {
            throw new JsonException(
                $"Unsupported medication event: {row.EventType}");
        }

        var instance = JsonSerializer.Deserialize(
            row.Payload,
            eventType,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        return instance as IDomainEvent
            ?? throw new JsonException(
                "Invalid medication event payload.");
    }
}
