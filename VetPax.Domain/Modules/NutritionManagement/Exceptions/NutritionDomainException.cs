namespace VetPax.Domain.Modules.NutritionManagement.Exceptions;

public sealed class NutritionDomainException : Exception
{
    public NutritionDomainException(string message)
        : base(message)
    {
    }
}