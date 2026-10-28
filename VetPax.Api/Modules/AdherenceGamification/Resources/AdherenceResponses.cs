using VetPax.Application.Modules.AdherenceGamification.Models;

namespace VetPax.Api.Modules.AdherenceGamification.Resources;

public sealed record RecognitionResponse(
    Guid RecognitionId,
    string FromLevel,
    string ToLevel,
    DateTime CreatedAtUtc,
    DateTime? SentAtUtc);

public sealed record AdherenceResponse(
    Guid OwnerId,
    bool HasSufficientEvidence,
    int EvidenceCount,
    decimal? Score,
    string? Level,
    IReadOnlyList<RecognitionResponse> Recognitions)
{
    public static AdherenceResponse From(AdherenceData data)
    {
        return new AdherenceResponse(
            data.OwnerId,
            data.HasSufficientEvidence,
            data.EvidenceCount,
            data.Score,
            data.Level,
            data.Recognitions.Select(x =>
                new RecognitionResponse(
                    x.RecognitionId,
                    x.FromLevel,
                    x.ToLevel,
                    x.CreatedAtUtc,
                    x.SentAtUtc)).ToArray());
    }
}