
using Microsoft.EntityFrameworkCore;
using VetPax.Application.Modules.AdherenceGamification.Commands;
using VetPax.Application.Modules.AdherenceGamification.Ports;
using VetPax.Infrastructure.Modules.AdherenceGamification.Persistence;
using VetPax.Infrastructure.Persistence;

namespace VetPax.Infrastructure.Modules.AdherenceGamification.Adapters;

public sealed class AdherenceEvidenceStore : IAdherenceEvidenceStore
{
    private readonly VetPaxDbContext _db;
    private readonly TimeProvider _clock;

    public AdherenceEvidenceStore(
        VetPaxDbContext db,
        TimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<bool> AlreadyProcessedAsync(
        IngestAdherenceEvidenceCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var kind = command.Kind.ToString();

        var tracked = _db.Set<ProcessedAdherenceEventRow>()
            .Local.Any(x =>
                x.EventId == command.EventId ||
                (x.Kind == kind &&
                 x.SourceId == command.SourceId));

        if (tracked)
            return true;

        return await _db.Set<ProcessedAdherenceEventRow>()
            .AsNoTracking()
            .AnyAsync(
                x => x.EventId == command.EventId ||
                    (x.Kind == kind &&
                     x.SourceId == command.SourceId),
                cancellationToken);
    }

    public async Task StageAsync(
        IngestAdherenceEvidenceCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        await _db.Set<ProcessedAdherenceEventRow>()
            .AddAsync(new ProcessedAdherenceEventRow
            {
                EventId = command.EventId,
                OwnerId = command.OwnerId,
                Kind = command.Kind.ToString(),
                SourceId = command.SourceId,
                OccurredAtUtc = command.OccurredAtUtc,
                ProcessedAtUtc = _clock.GetUtcNow().UtcDateTime
            }, cancellationToken);
    }

    public async Task<int> CountIncludingStagedAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        var persisted = await _db.Set<ProcessedAdherenceEventRow>()
            .AsNoTracking()
            .CountAsync(
                x => x.OwnerId == ownerId,
                cancellationToken);

        var staged = _db.Set<ProcessedAdherenceEventRow>()
            .Local.Count(x =>
                x.OwnerId == ownerId &&
                _db.Entry(x).State == EntityState.Added);

        return checked(persisted + staged);
    }
}
