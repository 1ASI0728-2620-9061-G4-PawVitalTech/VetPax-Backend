using VetPax.Domain.Shared.Events;

namespace VetPax.Domain.Modules.MedicationTreatment.Events;

public sealed record MedicationPlanPrescribedEvent(
    Guid PlanId,
    Guid PetId,
    Guid OwnerId,
    Guid ClinicId,
    Guid VeterinarianId,
    DateTime OccurredAtUtc
) : IDomainEvent
{
    DateTime IDomainEvent.OccurredAt => OccurredAtUtc;
}

public sealed record MedicationScheduleChangedEvent(
    Guid PlanId,
    int ScheduleVersion,
    DateTime OccurredAtUtc
) : IDomainEvent
{
    DateTime IDomainEvent.OccurredAt => OccurredAtUtc;
}

public sealed record MedicationRemindersConfiguredEvent(
    Guid PlanId,
    Guid OwnerId,
    bool Enabled,
    DateTime OccurredAtUtc
) : IDomainEvent
{
    DateTime IDomainEvent.OccurredAt => OccurredAtUtc;
}

public sealed record MedicationDoseScheduledEvent(
    Guid PlanId,
    Guid DoseId,
    Guid PetId,
    DateTime ScheduledAtUtc,
    DateTime OccurredAtUtc
) : IDomainEvent
{
    DateTime IDomainEvent.OccurredAt => OccurredAtUtc;
}

public sealed record MedicationReminderGeneratedEvent(
    Guid PlanId,
    Guid DoseId,
    Guid OwnerId,
    DateTime OccurredAtUtc
) : IDomainEvent
{
    DateTime IDomainEvent.OccurredAt => OccurredAtUtc;
}

public sealed record MedicationDoseAdministeredEvent(
    Guid EventId,
    Guid PlanId,
    Guid DoseId,
    Guid AdministrationId,
    Guid PetId,
    Guid OwnerId,
    DateTime ScheduledAtUtc,
    DateTime AdministeredAtUtc,
    DateTime OccurredAtUtc
) : IDomainEvent
{
    DateTime IDomainEvent.OccurredAt => OccurredAtUtc;
}

public sealed record MedicationPlanClosedEvent(
    Guid PlanId,
    string FinalStatus,
    DateTime OccurredAtUtc
) : IDomainEvent
{
    DateTime IDomainEvent.OccurredAt => OccurredAtUtc;
}