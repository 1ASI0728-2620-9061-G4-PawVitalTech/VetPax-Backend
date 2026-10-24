using VetPax.Domain.Modules.AppointmentManagement.ValueObjects;
using VetPax.Domain.Shared.Events;

namespace VetPax.Domain.Modules.AppointmentManagement.Events;

public sealed record AppointmentScheduledEvent(
    AppointmentId AppointmentId,
    AppointmentPetId PetId,
    AppointmentClinicId ClinicId,
    AppointmentVeterinarianId VeterinarianId,
    AppointmentSlot Slot,
    DateTime OccurredAt
) : IDomainEvent;

public sealed record AppointmentRescheduledEvent(
    AppointmentId AppointmentId,
    AppointmentSlot PreviousSlot,
    AppointmentSlot NewSlot,
    DateTime OccurredAt
) : IDomainEvent;

public sealed record AppointmentCancelledEvent(
    AppointmentId AppointmentId,
    AppointmentSlot ReleasedSlot,
    DateTime OccurredAt
) : IDomainEvent;

public sealed record AppointmentAttendedEvent(
    AppointmentId AppointmentId,
    AppointmentPetId PetId,
    AppointmentClinicId ClinicId,
    AppointmentVeterinarianId VeterinarianId,
    Guid ClinicalAttentionId,
    DateTime OccurredAt
) : IDomainEvent;