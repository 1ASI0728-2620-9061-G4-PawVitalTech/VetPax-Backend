using VetPax.Domain.Modules.NutritionManagement.Exceptions;

namespace VetPax.Domain.Modules.NutritionManagement.ValueObjects;

public sealed record FeedingPortion
{
    public decimal Quantity { get; }
    public string Unit { get; }

    public FeedingPortion(decimal quantity, string unit)
    {
        if (quantity <= 0)
            throw new NutritionDomainException(
                "Feeding quantity must be greater than zero.");

        if (string.IsNullOrWhiteSpace(unit))
            throw new NutritionDomainException(
                "Feeding unit is required.");

        if (unit.Trim().Length > 30)
            throw new NutritionDomainException(
                "Feeding unit cannot exceed 30 characters.");

        Quantity = quantity;
        Unit = unit.Trim();
    }
}