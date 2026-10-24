namespace VetPax.Api.Modules.NutritionManagement.BackgroundServices;

public sealed class NutritionWorkerOptions
{
    public const string SectionName = "NutritionManagement:Worker";

    public bool Enabled { get; set; } = false;
    public bool GenerationEnabled { get; set; } = false;
    public bool DeliveryEnabled { get; set; } = false;

    public int PollIntervalSeconds { get; set; } = 60;
    public int GenerateIntervalMinutes { get; set; } = 30;
    public int DaysAhead { get; set; } = 2;
    public int BatchSize { get; set; } = 20;

    public bool IsValid()
    {
        return PollIntervalSeconds is >= 15 and <= 300
               && GenerateIntervalMinutes is >= 1 and <= 1440
               && DaysAhead is >= 0 and <= 5
               && BatchSize is >= 1 and <= 100;
    }
}