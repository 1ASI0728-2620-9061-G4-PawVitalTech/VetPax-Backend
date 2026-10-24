namespace VetPax.Infrastructure.Modules.AdherenceGamification.Persistence;

public sealed class AdherenceRow
{
    public Guid OwnerId { get; set; }
    public int EvidenceCount { get; set; }
    public decimal? Score { get; set; }
    public string? Level { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public long ConcurrencyStamp { get; set; }
}

public sealed class ProcessedAdherenceEventRow
{
    public Guid EventId { get; set; }
    public Guid OwnerId { get; set; }
    public string Kind { get; set; } = string.Empty;
    public Guid SourceId { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public DateTime ProcessedAtUtc { get; set; }
}

public sealed class AdherenceRecognitionRow
{
    public Guid RecognitionId { get; set; }
    public Guid OwnerId { get; set; }
    public string FromLevel { get; set; } = string.Empty;
    public string ToLevel { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
    public DateTime? SentAtUtc { get; set; }

    public string DeliveryStatus { get; set; } = "Pending";
    public int AttemptCount { get; set; }
    public DateTime? NextAttemptAtUtc { get; set; }

    public Guid? LeaseId { get; set; }
    public DateTime? LockedUntilUtc { get; set; }
}

public sealed class AdherenceOutboxRow
{
    public Guid EventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;

    public DateTime OccurredAtUtc { get; set; }
    public DateTime? PublishedAtUtc { get; set; }

    public int AttemptCount { get; set; }
}