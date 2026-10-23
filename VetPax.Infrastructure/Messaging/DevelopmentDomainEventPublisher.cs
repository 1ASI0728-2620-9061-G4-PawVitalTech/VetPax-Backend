using Microsoft.Extensions.Logging;
using VetPax.Application.Common.Ports;
using VetPax.Domain.Shared.Events;

namespace VetPax.Infrastructure.Messaging;

public sealed class DevelopmentDomainEventPublisher
    : IDomainEventPublisher
{
    private readonly ILogger<DevelopmentDomainEventPublisher>
        _logger;

    public DevelopmentDomainEventPublisher(
        ILogger<DevelopmentDomainEventPublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync(
        IReadOnlyCollection<IDomainEvent> events,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(events);

        foreach (var domainEvent in events)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogInformation(
                "[DEV] Domain event captured: {EventType} at {OccurredAt}",
                domainEvent.GetType().Name,
                domainEvent.OccurredAt);
        }

        return Task.CompletedTask;
    }
}