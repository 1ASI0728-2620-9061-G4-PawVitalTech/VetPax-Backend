using VetPax.Domain.Modules.NutritionManagement.ValueObjects;
using VetPax.Domain.Shared.Events;

namespace VetPax.Domain.Modules.NutritionManagement.Events;

public sealed record FeedingPlanPrescribedEvent(
    FeedingPlanId PlanId,
    Guid PetId,
    Guid ClinicId,
    Guid VeterinarianId,
    DateTime OccurredAtUtc) : IDomainEvent
{
    public DateTime OccurredAt => OccurredAtUtc;
}

public sealed record FeedingPlanUpdatedEvent(
    FeedingPlanId PlanId,
    Guid PetId,
    int PreviousVersion,
    int CurrentVersion,
    DateTime OccurredAtUtc) : IDomainEvent
{
    public DateTime OccurredAt => OccurredAtUtc;
}

public sealed record FeedingPlanDeactivatedEvent(
    FeedingPlanId PlanId,
    Guid PetId,
    DateTime OccurredAtUtc) : IDomainEvent
{
    public DateTime OccurredAt => OccurredAtUtc;
}

public sealed record FeedingRemindersConfiguredEvent(
    FeedingPlanId PlanId,
    Guid PetId,
    bool Enabled,
    DateTime OccurredAtUtc) : IDomainEvent
{
    public DateTime OccurredAt => OccurredAtUtc;
}

public sealed record FeedingReminderGeneratedEvent(
    FeedingPlanId PlanId,
    Guid PetId,
    Guid ScheduleId,
    int PlanVersion,
    DateOnly LocalDate,
    DateTime OccurredAtUtc) : IDomainEvent
{
    public DateTime OccurredAt => OccurredAtUtc;
}