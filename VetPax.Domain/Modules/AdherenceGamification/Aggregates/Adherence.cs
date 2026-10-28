
using VetPax.Domain.Modules.AdherenceGamification.Entities;
using VetPax.Domain.Modules.AdherenceGamification.Enums;
using VetPax.Domain.Modules.AdherenceGamification.Events;
using VetPax.Domain.Modules.AdherenceGamification.Exceptions;
using VetPax.Domain.Modules.AdherenceGamification.ValueObjects;
using VetPax.Domain.Shared.Events;

namespace VetPax.Domain.Modules.AdherenceGamification.Aggregates;

public sealed class Adherence
{
    private readonly List<Recognition> _recognitions = new();
    private readonly List<IDomainEvent> _events = new();

    public Guid OwnerId { get; }

    public int EvidenceCount { get; private set; }

    public AdherenceScore? Score { get; private set; }

    public ConstancyLevel? Level { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public long PersistenceVersion { get; private set; }

    public IReadOnlyList<Recognition> Recognitions =>
        _recognitions.AsReadOnly();

    private Adherence(
        Guid ownerId,
        int evidenceCount,
        AdherenceScore? score,
        ConstancyLevel? level,
        DateTime updatedAtUtc,
        IEnumerable<Recognition> recognitions,
        long persistenceVersion)
    {
        ArgumentNullException.ThrowIfNull(recognitions);

        if (ownerId == Guid.Empty ||
            evidenceCount < 0 ||
            persistenceVersion < 0)
        {
            throw new AdherenceDomainException(
                "Invalid adherence profile identity or counters.");
        }

        ValidateUtc(updatedAtUtc);

        if (level.HasValue &&
            !Enum.IsDefined(level.Value))
        {
            throw new AdherenceDomainException(
                "Invalid constancy level.");
        }

        if (evidenceCount == 0 &&
            (score.HasValue || level.HasValue))
        {
            throw new AdherenceDomainException(
                "A profile without evidence cannot have a score or level.");
        }

        if (evidenceCount > 0 && !score.HasValue)
        {
            throw new AdherenceDomainException(
                "A profile with evidence requires a score.");
        }

        var recognitionList = recognitions.ToArray();

        if (recognitionList.Any(x =>
                x is null ||
                x.OwnerId != ownerId))
        {
            throw new AdherenceDomainException(
                "Recognition owner mismatch.");
        }

        if (recognitionList
                .Select(x => x.RecognitionId)
                .Distinct()
                .Count() != recognitionList.Length)
        {
            throw new AdherenceDomainException(
                "Duplicate recognition IDs.");
        }

        OwnerId = ownerId;
        EvidenceCount = evidenceCount;
        Score = score;
        Level = level;
        UpdatedAtUtc = updatedAtUtc;
        PersistenceVersion = persistenceVersion;

        _recognitions.AddRange(recognitionList);
    }

    public static Adherence Create(
        Guid ownerId,
        DateTime utcNow)
    {
        return new Adherence(
            ownerId,
            0,
            null,
            null,
            utcNow,
            Array.Empty<Recognition>(),
            0);
    }

    public static Adherence Rehydrate(
        Guid ownerId,
        int evidenceCount,
        AdherenceScore? score,
        ConstancyLevel? level,
        DateTime updatedAtUtc,
        IEnumerable<Recognition> recognitions,
        long persistenceVersion)
    {
        return new Adherence(
            ownerId,
            evidenceCount,
            score,
            level,
            updatedAtUtc,
            recognitions,
            persistenceVersion);
    }

    // TS10: Calculate constancy from verified evidence.
    public void Recalculate(
        int evidenceCount,
        AdherenceScore score,
        AdherencePolicy policy,
        DateTime occurredAtUtc)
    {
        ArgumentNullException.ThrowIfNull(policy);
        ValidateUtc(occurredAtUtc);

        if (evidenceCount < EvidenceCount)
        {
            throw new AdherenceDomainException(
                "Evidence count cannot decrease.");
        }

        if (occurredAtUtc < UpdatedAtUtc)
        {
            throw new AdherenceDomainException(
                "Evaluation timestamp cannot go backwards.");
        }

        var previous = Level;
        var next = policy.Evaluate(evidenceCount, score);

        if (!policy.AllowDemotion &&
            previous.HasValue &&
            (!next.HasValue || next.Value < previous.Value))
        {
            next = previous;
        }

        EvidenceCount = evidenceCount;
        Score = evidenceCount == 0 ? null : score;
        Level = next;
        UpdatedAtUtc = occurredAtUtc;

        _events.Add(new AdherenceCalculatedEvent(
            OwnerId,
            score.Value,
            evidenceCount,
            occurredAtUtc));

        if (previous == next)
            return;

        _events.Add(new ConstancyLevelChangedEvent(
            OwnerId,
            previous,
            next,
            occurredAtUtc));

        // Assigning the first level is NOT a promotion.
        if (!previous.HasValue ||
            !next.HasValue ||
            next.Value <= previous.Value)
        {
            return;
        }

        var recognition = new Recognition(
            Guid.NewGuid(),
            OwnerId,
            previous.Value,
            next.Value,
            occurredAtUtc);

        _recognitions.Add(recognition);

        _events.Add(new OwnerPromotedEvent(
            OwnerId,
            recognition.RecognitionId,
            previous.Value,
            next.Value,
            occurredAtUtc));
    }

    // US14: Register successful recognition delivery.
    public void MarkRecognitionSent(
        Guid recognitionId,
        DateTime sentAtUtc)
    {
        ValidateUtc(sentAtUtc);

        var recognition = _recognitions
            .SingleOrDefault(x =>
                x.RecognitionId == recognitionId);

        if (recognition is null)
        {
            throw new AdherenceDomainException(
                "Recognition not found.");
        }

        if (!recognition.MarkSent(sentAtUtc))
            return;

        _events.Add(new RecognitionSentEvent(
            OwnerId,
            recognitionId,
            sentAtUtc));
    }

    public IReadOnlyList<IDomainEvent> PullDomainEvents()
    {
        var events = _events.ToArray();
        _events.Clear();
        return events;
    }

    private static void ValidateUtc(DateTime value)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new AdherenceDomainException(
                "Domain timestamps must be UTC.");
        }
    }
}
