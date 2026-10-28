using VetPax.Domain.Modules.AdherenceGamification.Exceptions;

namespace VetPax.Domain.Modules.AdherenceGamification.ValueObjects;

public readonly record struct AdherenceScore
{
    public decimal Value { get; }

    public AdherenceScore(decimal value)
    {
        if (value < 0)
            throw new AdherenceDomainException(
                "Adherence score cannot be negative.");

        Value = value;
    }
}