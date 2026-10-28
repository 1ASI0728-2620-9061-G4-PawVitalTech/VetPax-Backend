using VetPax.Domain.Modules.AdherenceGamification.Enums;

namespace VetPax.Application.Modules.AdherenceGamification.Commands;

public sealed record IngestAdherenceEvidenceCommand(
    Guid EventId,
    Guid OwnerId,
    AdherenceEvidenceKind Kind,
    Guid SourceId,
    DateTime OccurredAtUtc);