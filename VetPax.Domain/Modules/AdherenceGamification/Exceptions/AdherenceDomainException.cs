namespace VetPax.Domain.Modules.AdherenceGamification.Exceptions;

public sealed class AdherenceDomainException : Exception
{
    public AdherenceDomainException(string message)
        : base(message)
    {
    }
}