namespace VetPax.Domain.Shared.Events;

public interface IDomainEvent
{
    DateTime OccurredAt { get; }
}