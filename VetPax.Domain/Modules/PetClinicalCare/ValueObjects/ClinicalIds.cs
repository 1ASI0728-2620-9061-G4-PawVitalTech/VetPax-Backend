namespace VetPax.Domain.Modules.PetClinicalCare.ValueObjects;

public readonly record struct ClinicalRecordId(Guid Value)
{
    public static ClinicalRecordId New()
        => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}

public readonly record struct AttentionId(Guid Value)
{
    public static AttentionId New()
        => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}

public readonly record struct ClinicId(Guid Value)
{
    public override string ToString() => Value.ToString();
}

public readonly record struct VeterinarianId(Guid Value)
{
    public override string ToString() => Value.ToString();
}