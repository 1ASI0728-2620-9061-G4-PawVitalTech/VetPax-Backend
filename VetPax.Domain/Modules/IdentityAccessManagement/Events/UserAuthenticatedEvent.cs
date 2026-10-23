using VetPax.Domain.Shared.Events;
using VetPax.Domain.Modules.IdentityAccessManagement.ValueObjects;

namespace VetPax.Domain.Modules.IdentityAccessManagement.Events;

public sealed record UserAuthenticatedEvent(
    UserId UserId,
    DateTime OccurredAt
) : IDomainEvent;