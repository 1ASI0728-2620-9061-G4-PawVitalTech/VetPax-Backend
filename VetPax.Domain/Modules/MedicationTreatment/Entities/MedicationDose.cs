
using VetPax.Domain.Modules.MedicationTreatment.Enums;
using VetPax.Domain.Modules.MedicationTreatment.Exceptions;
using VetPax.Domain.Modules.MedicationTreatment.ValueObjects;

namespace VetPax.Domain.Modules.MedicationTreatment.Entities;

public sealed class MedicationDose
{
    public MedicationDoseId DoseId { get; }
    public MedicationPlanId PlanId { get; }
    public MedicationScheduleId ScheduleId { get; }
    public int ScheduleVersion { get; }

    public DateOnly LocalDate { get; }
    public TimeOnly LocalTime { get; }
    public DateTime ScheduledAtUtc { get; }

    public MedicationDoseStatus Status { get; private set; }
    public DoseAdministration? Administration { get; private set; }

    private MedicationDose(
        MedicationDoseId doseId,
        MedicationPlanId planId,
        MedicationScheduleId scheduleId,
        int scheduleVersion,
        DateOnly localDate,
        TimeOnly localTime,
        DateTime scheduledAtUtc,
        MedicationDoseStatus status,
        DoseAdministration? administration)
    {
        DoseId = doseId;
        PlanId = planId;
        ScheduleId = scheduleId;
        ScheduleVersion = scheduleVersion;
        LocalDate = localDate;
        LocalTime = localTime;
        ScheduledAtUtc = scheduledAtUtc;
        Status = status;
        Administration = administration;
    }

    internal static MedicationDose Create(
        MedicationPlanId planId,
        MedicationSchedule schedule,
        int scheduleVersion,
        DateOnly localDate,
        DateTime scheduledAtUtc)
    {
        ArgumentNullException.ThrowIfNull(schedule);

        if (scheduledAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new MedicationDomainException(
                "Scheduled time must be in UTC.");
        }

        if (!schedule.AppliesOn(localDate))
        {
            throw new MedicationDomainException(
                "Schedule does not apply on the specified date.");
        }

        return new MedicationDose(
            MedicationDoseId.New(),
            planId,
            schedule.ScheduleId,
            scheduleVersion,
            localDate,
            schedule.LocalTime,
            scheduledAtUtc,
            MedicationDoseStatus.Pending,
            null);
    }

    internal DoseAdministration Administer(
        MedicationOwnerId ownerId,
        DateTime administeredAtUtc,
        DateTime recordedAtUtc)
    {
        if (Status == MedicationDoseStatus.Administered)
        {
            throw new MedicationDomainException(
                "Medication dose has already been administered.");
        }

        if (Status != MedicationDoseStatus.Pending)
        {
            throw new MedicationDomainException(
                "Only pending medication doses can be administered.");
        }

        if (ownerId.Value == Guid.Empty)
        {
            throw new MedicationDomainException(
                "A valid medication owner is required.");
        }

        if (administeredAtUtc.Kind != DateTimeKind.Utc ||
            recordedAtUtc.Kind != DateTimeKind.Utc ||
            administeredAtUtc > recordedAtUtc)
        {
            throw new MedicationDomainException(
                "Administration timestamps must be valid UTC values.");
        }

        var administration = new DoseAdministration(
            MedicationAdministrationId.New(),
            DoseId,
            ownerId,
            administeredAtUtc,
            recordedAtUtc);

        Administration = administration;
        Status = MedicationDoseStatus.Administered;

        return administration;
    }

    internal void CancelIfPending()
    {
        if (Status == MedicationDoseStatus.Pending)
        {
            Status = MedicationDoseStatus.Cancelled;
        }
    }

    public static MedicationDose Rehydrate(
        MedicationDoseId doseId,
        MedicationPlanId planId,
        MedicationScheduleId scheduleId,
        int scheduleVersion,
        DateOnly localDate,
        TimeOnly localTime,
        DateTime scheduledAtUtc,
        MedicationDoseStatus status,
        DoseAdministration? administration)
    {
        if (doseId.Value == Guid.Empty ||
            planId.Value == Guid.Empty ||
            scheduleId.Value == Guid.Empty ||
            scheduleVersion < 1 ||
            scheduledAtUtc.Kind != DateTimeKind.Utc ||
            !Enum.IsDefined(status))
        {
            throw new MedicationDomainException(
                "Invalid persisted medication dose.");
        }

        if ((status == MedicationDoseStatus.Administered) !=
            (administration is not null))
        {
            throw new MedicationDomainException(
                "Medication administration state is inconsistent.");
        }

        if (administration is not null &&
            administration.DoseId != doseId)
        {
            throw new MedicationDomainException(
                "Administration belongs to another dose.");
        }

        return new MedicationDose(
            doseId,
            planId,
            scheduleId,
            scheduleVersion,
            localDate,
            localTime,
            scheduledAtUtc,
            status,
            administration);
    }
}
