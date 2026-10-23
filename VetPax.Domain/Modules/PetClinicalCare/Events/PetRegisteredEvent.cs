using VetPax.Domain.Shared.Events;
using VetPax.Domain.Modules.PetClinicalCare.ValueObjects;

namespace VetPax.Domain.Modules.PetClinicalCare.Events;

public sealed record PetRegisteredEvent(
    PetId PetId,
    OwnerId OwnerId,
    DateTime OccurredAt
) : IDomainEvent;