using VetPax.Domain.Modules.AdherenceGamification.Enums;
using VetPax.Domain.Shared.Events;

namespace VetPax.Domain.Modules.AdherenceGamification.Events;

public sealed record AdherenceCalculatedEvent(
    Guid OwnerId,
    decimal Score,
    int EvidenceCount,
    DateTime OccurredAtUtc) : IDomainEvent
{
    public DateTime OccurredAt => OccurredAtUtc;
}

public sealed record ConstancyLevelChangedEvent(
    Guid OwnerId,
    ConstancyLevel? PreviousLevel,
    ConstancyLevel? CurrentLevel,
    DateTime OccurredAtUtc) : IDomainEvent
{
    public DateTime OccurredAt => OccurredAtUtc;
}

public sealed record OwnerPromotedEvent(
    Guid OwnerId,
    Guid RecognitionId,
    ConstancyLevel FromLevel,
    ConstancyLevel ToLevel,
    DateTime OccurredAtUtc) : IDomainEvent
{
    public DateTime OccurredAt => OccurredAtUtc;
}

public sealed record RecognitionSentEvent(
    Guid OwnerId,
    Guid RecognitionId,
    DateTime OccurredAtUtc) : IDomainEvent
{
    public DateTime OccurredAt => OccurredAtUtc;
}