using VetPax.Domain.Shared.Events;

namespace VetPax.Application.Common.Ports;

public interface IDomainEventPublisher
{
    Task PublishAsync(
        IReadOnlyCollection<IDomainEvent> events,
        CancellationToken cancellationToken = default);
}