using VetPax.Application.Modules.AdherenceGamification.Commands;
using VetPax.Domain.Modules.AdherenceGamification.ValueObjects;
using VetPax.Domain.Shared.Events;

namespace VetPax.Application.Modules.AdherenceGamification.Ports;

public interface IAdherenceEvidenceStore
{
    Task<bool> AlreadyProcessedAsync(
        IngestAdherenceEvidenceCommand command,
        CancellationToken cancellationToken = default);

    Task StageAsync(
        IngestAdherenceEvidenceCommand command,
        CancellationToken cancellationToken = default);

    Task<int> CountIncludingStagedAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default);
}

public interface IAdherenceOutbox
{
    Task StageAsync(
        IReadOnlyList<IDomainEvent> events,
        CancellationToken cancellationToken = default);
}

public interface IAdherenceUnitOfWork
{
    Task CommitAsync(
        CancellationToken cancellationToken = default);
}

public interface IAdherenceCurrentOwner
{
    Task<Guid> RequireOwnerIdAsync(
        CancellationToken cancellationToken = default);
}

public interface IAdherenceScoreCalculator
{
    Task<AdherenceScore> CalculateAsync(
        Guid ownerId,
        int evidenceCount,
        CancellationToken cancellationToken = default);
}