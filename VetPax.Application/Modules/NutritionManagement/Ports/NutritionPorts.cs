using VetPax.Application.Modules.NutritionManagement.Models;
using VetPax.Domain.Modules.NutritionManagement.Aggregates;
using VetPax.Domain.Shared.Events;

namespace VetPax.Application.Modules.NutritionManagement.Ports;

public enum NutritionUserRole
{
    Other = 0,
    Owner = 1,
    Veterinarian = 2
}

public sealed record NutritionActor(
    Guid UserId,
    NutritionUserRole Role);

public interface INutritionIdentityPort
{
    Task<NutritionActor?> GetCurrentAsync(
        CancellationToken cancellationToken = default);
}

public interface INutritionPatientPort
{
    Task<Guid?> GetOwnerIdAsync(
        Guid petId,
        CancellationToken cancellationToken = default);
}

public interface INutritionVeterinarianPort
{
    Task<bool> CanPrescribeAsync(
        Guid veterinarianId,
        Guid clinicId,
        Guid petId,
        CancellationToken cancellationToken = default);
}

public interface INutritionOutboxPort
{
    // Stage events in the same DbContext transaction
    // as the feeding plan changes.
    Task StageAsync(
        IReadOnlyList<IDomainEvent> events,
        CancellationToken cancellationToken = default);
}

public interface INutritionUnitOfWork
{
    Task CommitAsync(
        CancellationToken cancellationToken = default);
}

public interface INutritionReminderPort
{
    // True only when the reminder was newly staged.
    // The unique key must be enforced in MySQL.
    Task<bool> TryStageAsync(
        FeedingReminderOccurrence occurrence,
        CancellationToken cancellationToken = default);

    Task CancelPendingForPlanAsync(
        Guid planId,
        CancellationToken cancellationToken = default);
}

public interface INutritionReminderCandidatesPort
{
    Task<IReadOnlyList<FeedingPlan>> FindCandidatesAsync(
        int batchSize,
        CancellationToken cancellationToken = default);
}