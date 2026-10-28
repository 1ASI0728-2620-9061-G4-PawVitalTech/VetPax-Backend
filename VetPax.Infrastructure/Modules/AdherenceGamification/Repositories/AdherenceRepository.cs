
using Microsoft.EntityFrameworkCore;
using VetPax.Domain.Modules.AdherenceGamification.Aggregates;
using VetPax.Domain.Modules.AdherenceGamification.Entities;
using VetPax.Domain.Modules.AdherenceGamification.Enums;
using VetPax.Domain.Modules.AdherenceGamification.Repositories;
using VetPax.Domain.Modules.AdherenceGamification.ValueObjects;
using VetPax.Infrastructure.Modules.AdherenceGamification.Persistence;
using VetPax.Infrastructure.Persistence;

namespace VetPax.Infrastructure.Modules.AdherenceGamification.Repositories;

public sealed class AdherenceRepository : IAdherenceRepository
{
    private readonly VetPaxDbContext _db;

    public AdherenceRepository(VetPaxDbContext db)
    {
        _db = db;
    }

    public async Task<Adherence?> FindByOwnerAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        var row = await _db.Set<AdherenceRow>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.OwnerId == ownerId,
                cancellationToken);

        if (row is null)
            return null;

        var history = await _db.Set<AdherenceRecognitionRow>()
            .AsNoTracking()
            .Where(x => x.OwnerId == ownerId)
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var recognitions = history.Select(x =>
            new Recognition(
                x.RecognitionId,
                x.OwnerId,
                Enum.Parse<ConstancyLevel>(x.FromLevel),
                Enum.Parse<ConstancyLevel>(x.ToLevel),
                AsUtc(x.CreatedAtUtc),
                x.SentAtUtc.HasValue
                    ? AsUtc(x.SentAtUtc.Value)
                    : null))
            .ToArray();

        return Adherence.Rehydrate(
            row.OwnerId,
            row.EvidenceCount,
            row.Score.HasValue
                ? new AdherenceScore(row.Score.Value)
                : null,
            row.Level is null
                ? null
                : Enum.Parse<ConstancyLevel>(row.Level),
            AsUtc(row.UpdatedAtUtc),
            recognitions,
            row.ConcurrencyStamp);
    }

    public async Task StageSaveAsync(
        Adherence adherence,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(adherence);

        var row = await _db.Set<AdherenceRow>()
            .SingleOrDefaultAsync(
                x => x.OwnerId == adherence.OwnerId,
                cancellationToken);

        if (row is null)
        {
            if (adherence.PersistenceVersion != 0)
                throw new DbUpdateConcurrencyException(
                    "Adherence profile was removed concurrently.");

            row = new AdherenceRow
            {
                OwnerId = adherence.OwnerId,
                ConcurrencyStamp = 1
            };

            await _db.Set<AdherenceRow>()
                .AddAsync(row, cancellationToken);
        }
        else
        {
            if (row.ConcurrencyStamp !=
                adherence.PersistenceVersion)
            {
                throw new DbUpdateConcurrencyException(
                    "Adherence profile changed concurrently.");
            }

            row.ConcurrencyStamp++;
        }

        row.EvidenceCount = adherence.EvidenceCount;
        row.Score = adherence.Score?.Value;
        row.Level = adherence.Level?.ToString();
        row.UpdatedAtUtc = adherence.UpdatedAtUtc;

        var existing = await _db.Set<AdherenceRecognitionRow>()
            .Where(x => x.OwnerId == adherence.OwnerId)
            .ToListAsync(cancellationToken);

        var byId = existing.ToDictionary(x => x.RecognitionId);

        foreach (var recognition in adherence.Recognitions)
        {
            if (byId.TryGetValue(
                    recognition.RecognitionId,
                    out var saved))
            {
                // Recognitions are immutable except
                // for their delivery timestamp.
                if (saved.OwnerId != recognition.OwnerId ||
                    saved.FromLevel != recognition.FromLevel.ToString() ||
                    saved.ToLevel != recognition.ToLevel.ToString())
                {
                    throw new InvalidOperationException(
                        "Recognition history cannot be rewritten.");
                }

                if (recognition.SentAtUtc.HasValue &&
                    !saved.SentAtUtc.HasValue)
                {
                    saved.SentAtUtc = recognition.SentAtUtc;
                    saved.DeliveryStatus = "Sent";
                }

                continue;
            }

            await _db.Set<AdherenceRecognitionRow>()
                .AddAsync(new AdherenceRecognitionRow
                {
                    RecognitionId = recognition.RecognitionId,
                    OwnerId = recognition.OwnerId,
                    FromLevel = recognition.FromLevel.ToString(),
                    ToLevel = recognition.ToLevel.ToString(),
                    CreatedAtUtc = recognition.CreatedAtUtc,
                    SentAtUtc = recognition.SentAtUtc,
                    DeliveryStatus = recognition.SentAtUtc.HasValue
                        ? "Sent"
                        : "Pending",
                    AttemptCount = 0
                }, cancellationToken);
        }
    }

    private static DateTime AsUtc(DateTime value)
    {
        return DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }
}
