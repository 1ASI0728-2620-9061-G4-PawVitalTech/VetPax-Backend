namespace VetPax.Application.Modules.MedicationTreatment.Exceptions;

public sealed class MedicationValidationException : Exception
{
    public MedicationValidationException(string message)
        : base(message)
    {
    }
}

public sealed class MedicationConflictException : Exception
{
    public MedicationConflictException(string message)
        : base(message)
    {
    }
}