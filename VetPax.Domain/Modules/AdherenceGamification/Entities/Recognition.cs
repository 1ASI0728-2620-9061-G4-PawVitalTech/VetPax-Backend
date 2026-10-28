
using VetPax.Domain.Modules.AdherenceGamification.Enums;
using VetPax.Domain.Modules.AdherenceGamification.Exceptions;

namespace VetPax.Domain.Modules.AdherenceGamification.Entities;

public sealed class Recognition
{
    public Guid RecognitionId { get; }
    public Guid OwnerId { get; }

    public ConstancyLevel FromLevel { get; }
    public ConstancyLevel ToLevel { get; }

    public DateTime CreatedAtUtc { get; }
    public DateTime? SentAtUtc { get; private set; }

    public Recognition(
        Guid recognitionId,
        Guid ownerId,
        ConstancyLevel fromLevel,
        ConstancyLevel toLevel,
        DateTime createdAtUtc,
        DateTime? sentAtUtc = null)
    {
        if (recognitionId == Guid.Empty ||
            ownerId == Guid.Empty)
        {
            throw new AdherenceDomainException(
                "Valid recognition and owner IDs are required.");
        }

        if (!Enum.IsDefined(fromLevel) ||
            !Enum.IsDefined(toLevel) ||
            toLevel <= fromLevel)
        {
            throw new AdherenceDomainException(
                "Recognition requires an actual promotion.");
        }

        if (createdAtUtc.Kind != DateTimeKind.Utc ||
            (sentAtUtc.HasValue &&
             sentAtUtc.Value.Kind != DateTimeKind.Utc))
        {
            throw new AdherenceDomainException(
                "Recognition timestamps must be UTC.");
        }

        if (sentAtUtc.HasValue &&
            sentAtUtc.Value < createdAtUtc)
        {
            throw new AdherenceDomainException(
                "Sent time cannot precede creation.");
        }

        RecognitionId = recognitionId;
        OwnerId = ownerId;
        FromLevel = fromLevel;
        ToLevel = toLevel;
        CreatedAtUtc = createdAtUtc;
        SentAtUtc = sentAtUtc;
    }

    public bool MarkSent(DateTime sentAtUtc)
    {
        if (sentAtUtc.Kind != DateTimeKind.Utc ||
            sentAtUtc < CreatedAtUtc)
        {
            throw new AdherenceDomainException(
                "Invalid recognition sent timestamp.");
        }

        if (SentAtUtc.HasValue)
            return false;

        SentAtUtc = sentAtUtc;
        return true;
    }
}
