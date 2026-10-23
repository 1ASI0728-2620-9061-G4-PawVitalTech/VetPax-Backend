using VetPax.Domain.Shared.Events;
using VetPax.Domain.Modules.PetClinicalCare.ValueObjects;

namespace VetPax.Domain.Modules.PetClinicalCare.Events;

// A clinical attention was registered
public sealed record ClinicalAttentionRegisteredEvent(
    ClinicalRecordId ClinicalRecordId,
    PetId PetId,
    AttentionId AttentionId,
    ClinicId ClinicId,
    VeterinarianId VeterinarianId,
    DateTime OccurredAt
) : IDomainEvent;

// A clinical record was updated
public sealed record ClinicalRecordUpdatedEvent(
    ClinicalRecordId ClinicalRecordId,
    PetId PetId,
    DateTime OccurredAt
) : IDomainEvent;