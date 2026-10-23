namespace VetPax.Domain.Modules.PetClinicalCare.ValueObjects;

public readonly record struct OwnerId(Guid Value)
{
    public override string ToString() => Value.ToString();
}