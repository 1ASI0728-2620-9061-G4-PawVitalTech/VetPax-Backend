using VetPax.Application.Modules.AdherenceGamification.Models;
using VetPax.Application.Modules.AdherenceGamification.Ports;
using VetPax.Application.Modules.AdherenceGamification.Queries;
using VetPax.Domain.Modules.AdherenceGamification.Repositories;
using VetPax.Domain.Modules.AdherenceGamification.ValueObjects;

namespace VetPax.Application.Modules.AdherenceGamification.Handlers;

public sealed class GetMyAdherenceHandler
{
    private readonly IAdherenceCurrentOwner _currentOwner;
    private readonly IAdherenceRepository _repository;
    private readonly AdherencePolicy _policy;

    public GetMyAdherenceHandler(
        IAdherenceCurrentOwner currentOwner,
        IAdherenceRepository repository,
        AdherencePolicy policy)
    {
        _currentOwner = currentOwner;
        _repository = repository;
        _policy = policy;
    }

    public async Task<AdherenceData> HandleAsync(
        GetMyAdherenceQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var ownerId = await _currentOwner.RequireOwnerIdAsync(
            cancellationToken);

        var adherence = await _repository.FindByOwnerAsync(
            ownerId,
            cancellationToken);

        return AdherenceData.From(
            adherence,
            ownerId,
            _policy);
    }
}