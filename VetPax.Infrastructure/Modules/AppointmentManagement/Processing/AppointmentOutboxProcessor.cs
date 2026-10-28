
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VetPax.Application.Common.Ports;
using VetPax.Domain.Modules.AppointmentManagement.Events;
using VetPax.Domain.Shared.Events;
using VetPax.Infrastructure.Modules.AppointmentManagement.Persistence;
using VetPax.Infrastructure.Persistence;

namespace VetPax.Infrastructure.Modules.AppointmentManagement.Processing;

public sealed class AppointmentOutboxProcessor
{
    private readonly VetPaxDbContext _db;
    private readonly IDomainEventPublisher _publisher;
    private readonly TimeProvider _clock;
    private readonly ILogger<AppointmentOutboxProcessor> _logger;

    public AppointmentOutboxProcessor(
        VetPaxDbContext db,
        IDomainEventPublisher publisher,
        TimeProvider clock,
        ILogger<AppointmentOutboxProcessor> logger)
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

        var due = await _db.AppointmentOutbox
            .AsNoTracking()
            .Where(x =>
                x.PublishedAtUtc == null &&
                x.NextAttemptAtUtc <= now)
            .OrderBy(x => x.OccurredAtUtc)
            .Take(20)
            .Select(x => new
            {
                x.EventId,
                x.Attempts
            })
            .ToListAsync(cancellationToken);

        var published = 0;

        foreach (var item in due)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Reserva temporal: otro worker no debe
            // adquirir este evento durante cinco minutos.
            var leaseUntil = _clock.GetUtcNow()
                .UtcDateTime
                .AddMinutes(5);

            var claimed = await _db.AppointmentOutbox
                .Where(x =>
                    x.EventId == item.EventId &&
                    x.PublishedAtUtc == null &&
                    x.NextAttemptAtUtc <= now)
                .ExecuteUpdateAsync(
                    update => update
                        .SetProperty(
                            x => x.NextAttemptAtUtc,
                            leaseUntil)
                        .SetProperty(
                            x => x.Attempts,
                            x => x.Attempts + 1),
                    cancellationToken);

            if (claimed != 1)
                continue;

            try
            {
                var row = await _db.AppointmentOutbox
                    .AsNoTracking()
                    .SingleAsync(
                        x => x.EventId == item.EventId,
                        cancellationToken);

                var domainEvent = DeserializeEvent(row);

                await _publisher.PublishAsync(
                    new IDomainEvent[] { domainEvent },
                    cancellationToken);

                var completedAt = _clock.GetUtcNow()
                    .UtcDateTime;

                var completed = await _db.AppointmentOutbox
                    .Where(x =>
                        x.EventId == item.EventId &&
                        x.PublishedAtUtc == null &&
                        x.NextAttemptAtUtc == leaseUntil)
                    .ExecuteUpdateAsync(
                        update => update
                            .SetProperty(
                                x => x.PublishedAtUtc,
                                completedAt)
                            .SetProperty(
                                x => x.LastError,
                                (string?)null),
                        cancellationToken);

                if (completed == 1)
                    published++;
            }
            catch (Exception ex)
                when (ex is not OperationCanceledException)
            {
                _logger.LogError(
                    ex,
                    "Appointment outbox event {EventId} failed.",
                    item.EventId);

                var retryAt = _clock.GetUtcNow()
                    .UtcDateTime
                    .AddMinutes(BackoffMinutes(item.Attempts + 1));

                var error = ex.Message.Length > 1024
                    ? ex.Message[..1024]
                    : ex.Message;

                await _db.AppointmentOutbox
                    .Where(x =>
                        x.EventId == item.EventId &&
                        x.PublishedAtUtc == null &&
                        x.NextAttemptAtUtc == leaseUntil)
                    .ExecuteUpdateAsync(
                        update => update
                            .SetProperty(
                                x => x.NextAttemptAtUtc,
                                retryAt)
                            .SetProperty(
                                x => x.LastError,
                                error),
                        cancellationToken);
            }
        }

        return published;
    }

    private static int BackoffMinutes(int attempts)
    {
        return Math.Min(
            60,
            1 << Math.Min(Math.Max(attempts - 1, 0), 6));
    }

    private static IDomainEvent DeserializeEvent(
        AppointmentOutboxRow row)
    {
        var eventType = row.EventType;

        Type type;

        if (eventType ==
            typeof(AppointmentScheduledEvent).FullName)
        {
            type = typeof(AppointmentScheduledEvent);
        }
        else if (eventType ==
                 typeof(AppointmentRescheduledEvent).FullName)
        {
            type = typeof(AppointmentRescheduledEvent);
        }
        else if (eventType ==
                 typeof(AppointmentCancelledEvent).FullName)
        {
            type = typeof(AppointmentCancelledEvent);
        }
        else if (eventType ==
                 typeof(AppointmentAttendedEvent).FullName)
        {
            type = typeof(AppointmentAttendedEvent);
        }
        else
        {
            throw new JsonException(
                $"Unsupported appointment event: {eventType}");
        }

        var deserialized = JsonSerializer.Deserialize(
            row.Payload,
            type,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        return deserialized as IDomainEvent
            ?? throw new JsonException(
                "Outbox payload is not a valid domain event.");
    }
}
