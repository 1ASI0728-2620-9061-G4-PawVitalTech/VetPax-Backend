using VetPax.Application.Modules.AdherenceGamification.Ports;
using VetPax.Domain.Modules.AdherenceGamification.ValueObjects;

namespace VetPax.Application.Modules.AdherenceGamification.Services;

public sealed class EvidenceCountScoreCalculator
    : IAdherenceScoreCalculator
{
    public Task<AdherenceScore> CalculateAsync(
        Guid ownerId,
        int evidenceCount,
        CancellationToken cancellationToken = default)
    {
        if (ownerId == Guid.Empty || evidenceCount < 0)
        {
            throw new ArgumentException(
                "Invalid adherence score input.");
        }

        // Provisional: one verified activity
        // contributes one score unit.
        return Task.FromResult(
            new AdherenceScore(evidenceCount));
    }
}