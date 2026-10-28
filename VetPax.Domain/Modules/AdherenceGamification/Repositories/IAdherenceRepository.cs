using VetPax.Domain.Modules.AdherenceGamification.Aggregates;

namespace VetPax.Domain.Modules.AdherenceGamification.Repositories;

public interface IAdherenceRepository
{
    Task<Adherence?> FindByOwnerAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default);

    Task StageSaveAsync(
        Adherence adherence,
        CancellationToken cancellationToken = default);
}