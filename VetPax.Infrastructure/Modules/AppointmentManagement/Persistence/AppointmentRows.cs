
using VetPax.Domain.Modules.AppointmentManagement.Enums;

namespace VetPax.Infrastructure.Modules.AppointmentManagement.Persistence;

public sealed class AppointmentRow
{
    public Guid AppointmentId { get; set; }

    public Guid OwnerId { get; set; }
    public Guid PetId { get; set; }
    public Guid ClinicId { get; set; }
    public Guid VeterinarianId { get; set; }

    public DateOnly AppointmentDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    public string Reason { get; set; } = string.Empty;

    public AppointmentStatus Status { get; set; }

    // Columna generada por MySQL. Nunca asignarla manualmente.
    public TimeOnly? ActiveSlotTime { get; private set; }

    public Guid? ClinicalAttentionId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Control de concurrencia optimista.
    public long Version { get; set; } = 1;
}

public sealed class AppointmentOutboxRow
{
    public Guid EventId { get; set; }
    public Guid AppointmentId { get; set; }

    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;

    public DateTime OccurredAtUtc { get; set; }

    public DateTime NextAttemptAtUtc { get; set; }

    public DateTime? PublishedAtUtc { get; set; }

    public int Attempts { get; set; }

    public string? LastError { get; set; }
}

public enum AppointmentReminderStatus
{
    Pending,
    Sent,
    Cancelled
}

public sealed class AppointmentReminderRow
{
    public Guid AppointmentId { get; set; }
    public Guid OwnerId { get; set; }

    public DateTime DueAtUtc { get; set; }

    public AppointmentReminderStatus Status { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public DateTime? SentAtUtc { get; set; }
}

public sealed class AppointmentAttentionLinkRow
{
    public Guid AppointmentId { get; set; }

    public Guid ClinicalAttentionId { get; set; }

    public DateTime LinkedAtUtc { get; set; }
}
