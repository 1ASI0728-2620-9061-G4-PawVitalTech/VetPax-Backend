
using VetPax.Domain.Modules.MedicationTreatment.Enums;

namespace VetPax.Infrastructure.Modules.MedicationTreatment.Persistence;

public sealed class MedicationPlanRow
{
    public Guid PlanId { get; set; }
    public Guid OwnerId { get; set; }
    public Guid PetId { get; set; }
    public Guid ClinicId { get; set; }
    public Guid VeterinarianId { get; set; }
    public Guid? ClinicalAttentionId { get; set; }

    public string MedicationName { get; set; } = "";
    public string Route { get; set; } = "";
    public string Instructions { get; set; } = "";
    public string TimeZoneId { get; set; } = "";

    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    public MedicationPlanStatus Status { get; set; }
    public bool RemindersEnabled { get; set; }
    public int ScheduleVersion { get; set; }

    public long Version { get; set; } = 1;

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public sealed class MedicationScheduleRow
{
    public Guid ScheduleId { get; set; }
    public Guid PlanId { get; set; }
    public int ScheduleVersion { get; set; }

    public TimeOnly LocalTime { get; set; }
    public int DaysMask { get; set; }

    public decimal Amount { get; set; }
    public string Unit { get; set; } = "";
}

public sealed class MedicationDoseRow
{
    public Guid DoseId { get; set; }
    public Guid PlanId { get; set; }
    public Guid ScheduleId { get; set; }
    public int ScheduleVersion { get; set; }

    public DateOnly LocalDate { get; set; }
    public TimeOnly LocalTime { get; set; }
    public DateTime ScheduledAtUtc { get; set; }

    public MedicationDoseStatus Status { get; set; }
}

public sealed class MedicationAdministrationRow
{
    public Guid AdministrationId { get; set; }
    public Guid DoseId { get; set; }
    public Guid AdministeredBy { get; set; }

    public DateTime AdministeredAtUtc { get; set; }
    public DateTime RecordedAtUtc { get; set; }
}

public sealed class MedicationReminderPreferenceRow
{
    public Guid PlanId { get; set; }
    public bool Enabled { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public enum MedicationReminderDeliveryStatus
{
    Pending = 1,
    Sent = 2,
    Cancelled = 3
}

public sealed class MedicationReminderRow
{
    public Guid ReminderId { get; set; }
    public Guid PlanId { get; set; }
    public Guid DoseId { get; set; }
    public Guid OwnerId { get; set; }

    public DateTime DueAtUtc { get; set; }
    public DateTime NextAttemptAtUtc { get; set; }

    public MedicationReminderDeliveryStatus Status { get; set; }

    public int Attempts { get; set; }
    public string? LastError { get; set; }

    public DateTime? SentAtUtc { get; set; }
    public DateTime? LockedUntilUtc { get; set; }
    public Guid? LeaseToken { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}

public sealed class MedicationOutboxRow
{
    public Guid EventId { get; set; }
    public Guid? PlanId { get; set; }

    public string EventType { get; set; } = "";
    public string Payload { get; set; } = "";

    public DateTime OccurredAtUtc { get; set; }
    public DateTime NextAttemptAtUtc { get; set; }
    public DateTime? PublishedAtUtc { get; set; }

    public int Attempts { get; set; }
    public string? LastError { get; set; }

    public Guid? LeaseToken { get; set; }
    public DateTime? LockedUntilUtc { get; set; }
}
