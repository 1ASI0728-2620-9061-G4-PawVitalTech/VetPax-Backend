
using VetPax.Domain.Modules.NutritionManagement.Enums;

namespace VetPax.Infrastructure.Modules.NutritionManagement.Persistence;

public sealed class FeedingPlanRow
{
    public Guid PlanId { get; set; }
    public Guid PetId { get; set; }
    public Guid ClinicId { get; set; }
    public Guid VeterinarianId { get; set; }

    public string FoodName { get; set; } = string.Empty;
    public string? Recommendations { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string TimeZoneId { get; set; } = string.Empty;

    public FeedingPlanStatus Status { get; set; }
    public bool RemindersEnabled { get; set; }

    public int Version { get; set; }
    public long ConcurrencyStamp { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public sealed class FeedingActivePlanRow
{
    public Guid PetId { get; set; }
    public Guid PlanId { get; set; }
}

public sealed class FeedingScheduleRow
{
    public Guid PlanId { get; set; }
    public int PlanVersion { get; set; }
    public Guid ScheduleId { get; set; }

    public TimeOnly LocalTime { get; set; }
    public string DaysOfWeek { get; set; } = string.Empty;

    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
}

public sealed class FeedingPlanRevisionRow
{
    public Guid PlanId { get; set; }
    public int Version { get; set; }

    public string FoodName { get; set; } = string.Empty;
    public string? Recommendations { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string TimeZoneId { get; set; } = string.Empty;

    public DateTime ReplacedAtUtc { get; set; }
}

public sealed class FeedingReminderRow
{
    public Guid ReminderId { get; set; }

    public Guid PlanId { get; set; }
    public Guid PetId { get; set; }
    public Guid ScheduleId { get; set; }
    public int PlanVersion { get; set; }

    public DateOnly LocalDate { get; set; }
    public DateTime ScheduledAtUtc { get; set; }

    public string Status { get; set; } = "Pending";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
    
    public int AttemptCount { get; set; }

    public DateTime? NextAttemptAtUtc { get; set; }

    public Guid? LeaseId { get; set; }

    public DateTime? LockedUntilUtc { get; set; }

    public string? LastError { get; set; }

}

public sealed class FeedingOutboxRow
{
    public Guid EventId { get; set; }

    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;

    public DateTime OccurredAtUtc { get; set; }
    public DateTime? PublishedAtUtc { get; set; }

    public int AttemptCount { get; set; }
    public DateTime? NextAttemptAtUtc { get; set; }
}
