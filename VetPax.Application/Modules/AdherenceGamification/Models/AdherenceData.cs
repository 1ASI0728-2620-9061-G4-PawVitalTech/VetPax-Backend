
using VetPax.Domain.Modules.AdherenceGamification.Aggregates;
using VetPax.Domain.Modules.AdherenceGamification.Entities;
using VetPax.Domain.Modules.AdherenceGamification.Enums;
using VetPax.Domain.Modules.AdherenceGamification.ValueObjects;

namespace VetPax.Application.Modules.AdherenceGamification.Models;

public sealed record RecognitionData(
    Guid RecognitionId,
    string FromLevel,
    string ToLevel,
    DateTime CreatedAtUtc,
    DateTime? SentAtUtc);

public sealed record AdherenceData(
    Guid OwnerId,
    bool HasSufficientEvidence,
    int EvidenceCount,
    decimal? Score,
    string? Level,
    IReadOnlyList<RecognitionData> Recognitions)
{
    public static AdherenceData From(
        Adherence? adherence,
        Guid ownerId,
        AdherencePolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        if (adherence is null)
        {
            return new AdherenceData(
                ownerId,
                false,
                0,
                null,
                null,
                Array.Empty<RecognitionData>());
        }

        return new AdherenceData(
            adherence.OwnerId,
            adherence.EvidenceCount >= policy.MinimumEvidence,
            adherence.EvidenceCount,
            adherence.Score?.Value,
            adherence.Level.HasValue
                ? Display(adherence.Level.Value)
                : null,
            adherence.Recognitions
                .Select(MapRecognition)
                .ToArray());
    }

    private static RecognitionData MapRecognition(
        Recognition recognition)
    {
        return new RecognitionData(
            recognition.RecognitionId,
            Display(recognition.FromLevel),
            Display(recognition.ToLevel),
            recognition.CreatedAtUtc,
            recognition.SentAtUtc);
    }

    private static string Display(ConstancyLevel level)
    {
        return level switch
        {
            ConstancyLevel.Bronze => "Bronce",
            ConstancyLevel.Silver => "Plata",
            ConstancyLevel.Gold => "Oro",
            _ => throw new ArgumentOutOfRangeException(
                nameof(level))
        };
    }
}
