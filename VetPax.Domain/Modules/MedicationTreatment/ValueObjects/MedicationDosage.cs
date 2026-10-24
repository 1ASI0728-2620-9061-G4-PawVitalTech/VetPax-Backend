using VetPax.Domain.Modules.MedicationTreatment.Exceptions;

namespace VetPax.Domain.Modules.MedicationTreatment.ValueObjects;

public sealed record MedicationDosage
{
    public decimal Amount { get; }
    public string Unit { get; }

    public MedicationDosage(decimal amount, string unit)
    {
        if (amount <= 0)
        {
            throw new MedicationDomainException(
                "Medication dosage amount must be positive.");
        }

        if (string.IsNullOrWhiteSpace(unit) ||
            unit.Trim().Length > 30)
        {
            throw new MedicationDomainException(
                "A valid dosage unit is required.");
        }

        Amount = amount;
        Unit = unit.Trim();
    }
}