namespace VetPax.Domain.Modules.ClinicManagement.ValueObjects;

public readonly record struct ClinicId(Guid Value)
{
    public static ClinicId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}

public readonly record struct AdministratorId(Guid Value)
{
    public override string ToString() => Value.ToString();
}