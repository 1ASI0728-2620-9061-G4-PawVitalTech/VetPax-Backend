namespace VetPax.Domain.Modules.PetClinicalCare.ValueObjects;

public sealed record ClinicalIndicator
{
    public string Name { get; }
    public decimal Value { get; }
    public string Unit { get; }

    public ClinicalIndicator(
        string name,
        decimal value,
        string unit)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Indicator name is required.");

        if (string.IsNullOrWhiteSpace(unit))
            throw new ArgumentException("Indicator unit is required.");

        Name = name.Trim();
        Value = value;
        Unit = unit.Trim();
    }

    public bool IsComparableWith(ClinicalIndicator? other)
    {
        return other is not null
               && string.Equals(Name, other.Name,
                   StringComparison.OrdinalIgnoreCase)
               && string.Equals(Unit, other.Unit,
                   StringComparison.OrdinalIgnoreCase);
    }
}