namespace VetPax.Domain.Modules.IdentityAccessManagement.ValueObjects;

public readonly record struct UserId(Guid Value)
{
    public override string ToString() => Value.ToString();
}