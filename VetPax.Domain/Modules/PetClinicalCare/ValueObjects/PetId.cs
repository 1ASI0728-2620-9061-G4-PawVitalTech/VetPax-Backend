namespace VetPax.Domain.Modules.PetClinicalCare.ValueObjects;

public readonly record struct PetId(Guid Value)
{
    public static PetId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}