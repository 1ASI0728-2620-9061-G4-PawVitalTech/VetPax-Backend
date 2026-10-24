using VetPax.Domain.Modules.AdherenceGamification.Enums;
using VetPax.Domain.Modules.AdherenceGamification.Exceptions;

namespace VetPax.Domain.Modules.AdherenceGamification.ValueObjects;

public sealed class AdherencePolicy
{
    public int MinimumEvidence { get; }
    public decimal BronzeThreshold { get; }
    public decimal SilverThreshold { get; }
    public decimal GoldThreshold { get; }
    public bool AllowDemotion { get; }

    public AdherencePolicy(
        int minimumEvidence,
        decimal bronzeThreshold,
        decimal silverThreshold,
        decimal goldThreshold,
        bool allowDemotion)
    {
        if (minimumEvidence < 1 ||
            bronzeThreshold < 0 ||
            silverThreshold <= bronzeThreshold ||
            goldThreshold <= silverThreshold)
        {
            throw new AdherenceDomainException(
                "Invalid adherence policy thresholds.");
        }

        MinimumEvidence = minimumEvidence;
        BronzeThreshold = bronzeThreshold;
        SilverThreshold = silverThreshold;
        GoldThreshold = goldThreshold;
        AllowDemotion = allowDemotion;
    }

    public ConstancyLevel? Evaluate(
        int evidenceCount,
        AdherenceScore score)
    {
        if (evidenceCount < 0)
            throw new AdherenceDomainException(
                "Evidence count cannot be negative.");

        if (evidenceCount < MinimumEvidence)
            return null;

        if (score.Value >= GoldThreshold)
            return ConstancyLevel.Gold;

        if (score.Value >= SilverThreshold)
            return ConstancyLevel.Silver;

        if (score.Value >= BronzeThreshold)
            return ConstancyLevel.Bronze;

        return null;
    }
}