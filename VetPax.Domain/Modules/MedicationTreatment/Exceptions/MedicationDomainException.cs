namespace VetPax.Domain.Modules.MedicationTreatment.Exceptions;

public sealed class MedicationDomainException : Exception
{
    public MedicationDomainException(string message)
        : base(message)
    {
    }
}