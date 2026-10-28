namespace VetPax.Domain.Modules.NutritionManagement.ValueObjects;

public readonly record struct FeedingPlanId(Guid Value)
{
    public static FeedingPlanId New()
        => new(Guid.NewGuid());

    public override string ToString()
        => Value.ToString("D");
}