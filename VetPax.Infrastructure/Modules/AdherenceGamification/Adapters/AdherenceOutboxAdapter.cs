using System.Text.Json;
using VetPax.Application.Modules.AdherenceGamification.Ports;
using VetPax.Domain.Shared.Events;
using VetPax.Infrastructure.Modules.AdherenceGamification.Persistence;
using VetPax.Infrastructure.Persistence;

namespace VetPax.Infrastructure.Modules.AdherenceGamification.Adapters;

public sealed class AdherenceOutboxAdapter : IAdherenceOutbox
{
    private readonly VetPaxDbContext _db;

    public AdherenceOutboxAdapter(VetPaxDbContext db)
    {
        _db = db;
    }

    public async Task StageAsync(
        IReadOnlyList<IDomainEvent> events,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(events);

        foreach (var domainEvent in events)
        {
            var type = domainEvent.GetType();

            await _db.Set<AdherenceOutboxRow>()
                .AddAsync(new AdherenceOutboxRow
                {
                    EventId = Guid.NewGuid(),
                    EventType = type.FullName ?? type.Name,
                    PayloadJson = JsonSerializer.Serialize(
                        domainEvent,
                        type),
                    OccurredAtUtc = domainEvent.OccurredAt,
                    AttemptCount = 0
                }, cancellationToken);
        }
    }
}