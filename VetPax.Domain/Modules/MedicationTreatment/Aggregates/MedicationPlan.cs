
using VetPax.Domain.Modules.MedicationTreatment.Entities;
using VetPax.Domain.Modules.MedicationTreatment.Enums;
using VetPax.Domain.Modules.MedicationTreatment.Events;
using VetPax.Domain.Modules.MedicationTreatment.Exceptions;
using VetPax.Domain.Modules.MedicationTreatment.ValueObjects;
using VetPax.Domain.Shared.Events;

namespace VetPax.Domain.Modules.MedicationTreatment.Aggregates;

public sealed class MedicationPlan
{
    private readonly List<MedicationSchedule> _schedules;
    private readonly List<MedicationDose> _doses;
    private readonly List<IDomainEvent> _domainEvents = new();

    public MedicationPlanId PlanId { get; }

    public MedicationOwnerId OwnerId { get; }
    public MedicationPetId PetId { get; }
    public MedicationClinicId ClinicId { get; }
    public MedicationVeterinarianId VeterinarianId { get; }

    public Guid? ClinicalAttentionId { get; }

    public string MedicationName { get; }
    public string Route { get; }
    public string Instructions { get; }

    public DateOnly StartDate { get; }
    public DateOnly? EndDate { get; }

    public string TimeZoneId { get; }

    public MedicationPlanStatus Status { get; private set; }

    public bool RemindersEnabled { get; private set; }

    public int ScheduleVersion { get; private set; }

    public DateTime CreatedAtUtc { get; }
    public DateTime UpdatedAtUtc { get; private set; }

    public IReadOnlyList<MedicationSchedule> Schedules =>
        _schedules.AsReadOnly();

    public IReadOnlyList<MedicationDose> Doses =>
        _doses.AsReadOnly();

    private MedicationPlan(
        MedicationPlanId planId,
        MedicationOwnerId ownerId,
        MedicationPetId petId,
        MedicationClinicId clinicId,
        MedicationVeterinarianId veterinarianId,
        Guid? clinicalAttentionId,
        string medicationName,
        string route,
        string instructions,
        DateOnly startDate,
        DateOnly? endDate,
        string timeZoneId,
        MedicationPlanStatus status,
        bool remindersEnabled,
        int scheduleVersion,
        DateTime createdAtUtc,
        DateTime updatedAtUtc,
        IEnumerable<MedicationSchedule> schedules,
        IEnumerable<MedicationDose> doses)
    {
        PlanId = planId;
        OwnerId = ownerId;
        PetId = petId;
        ClinicId = clinicId;
        VeterinarianId = veterinarianId;

        ClinicalAttentionId = clinicalAttentionId;

        MedicationName = medicationName;
        Route = route;
        Instructions = instructions;

        StartDate = startDate;
        EndDate = endDate;
        TimeZoneId = timeZoneId;

        Status = status;
        RemindersEnabled = remindersEnabled;
        ScheduleVersion = scheduleVersion;

        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;

        _schedules = schedules.ToList();
        _doses = doses.ToList();
    }

    // -------------------------------------------------
    // Factory: registrar prescripción
    // -------------------------------------------------

    public static MedicationPlan Prescribe(
        MedicationOwnerId ownerId,
        MedicationPetId petId,
        MedicationClinicId clinicId,
        MedicationVeterinarianId veterinarianId,
        string medicationName,
        string route,
        string instructions,
        DateOnly startDate,
        DateOnly? endDate,
        string timeZoneId,
        IReadOnlyCollection<MedicationSchedule> schedules,
        DateTime occurredAtUtc,
        Guid? clinicalAttentionId = null)
    {
        ValidateIdentifiers(
            ownerId,
            petId,
            clinicId,
            veterinarianId);

        ValidatePrescription(
            medicationName,
            route,
            instructions,
            startDate,
            endDate,
            timeZoneId,
            occurredAtUtc,
            clinicalAttentionId);

        ArgumentNullException.ThrowIfNull(schedules);

        ValidateSchedules(schedules);

        var plan = new MedicationPlan(
            MedicationPlanId.New(),
            ownerId,
            petId,
            clinicId,
            veterinarianId,
            clinicalAttentionId,
            medicationName.Trim(),
            route.Trim(),
            instructions?.Trim() ?? string.Empty,
            startDate,
            endDate,
            timeZoneId.Trim(),
            MedicationPlanStatus.Active,
            remindersEnabled: false,
            scheduleVersion: 1,
            createdAtUtc: occurredAtUtc,
            updatedAtUtc: occurredAtUtc,
            schedules,
            Array.Empty<MedicationDose>());

        plan._domainEvents.Add(
            new MedicationPlanPrescribedEvent(
                plan.PlanId.Value,
                petId.Value,
                ownerId.Value,
                clinicId.Value,
                veterinarianId.Value,
                occurredAtUtc));

        return plan;
    }

    // -------------------------------------------------
    // Seguridad y validación de estado
    // -------------------------------------------------

    public bool IsOwnedBy(MedicationOwnerId ownerId)
    {
        return OwnerId == ownerId;
    }

    public bool IsPrescribedBy(
        MedicationVeterinarianId veterinarianId)
    {
        return VeterinarianId == veterinarianId;
    }

    public bool IsActiveOn(DateOnly localDate)
    {
        return Status == MedicationPlanStatus.Active &&
               localDate >= StartDate &&
               (!EndDate.HasValue ||
                localDate <= EndDate.Value);
    }

    private void EnsureActive()
    {
        if (Status != MedicationPlanStatus.Active)
        {
            throw new MedicationDomainException(
                "Medication plan is not active.");
        }
    }

    private static void EnsureUtc(DateTime timestamp)
    {
        if (timestamp.Kind != DateTimeKind.Utc)
        {
            throw new MedicationDomainException(
                "Timestamp must be expressed in UTC.");
        }
    }

    // -------------------------------------------------
    // US07: configurar recordatorios
    // -------------------------------------------------

    public void ConfigureReminders(
        bool enabled,
        DateTime occurredAtUtc)
    {
        EnsureUtc(occurredAtUtc);
        EnsureActive();

        if (enabled && _schedules.Count == 0)
        {
            throw new MedicationDomainException(
                "Medication reminders require defined schedules.");
        }

        if (RemindersEnabled == enabled)
            return;

        RemindersEnabled = enabled;
        UpdatedAtUtc = occurredAtUtc;

        _domainEvents.Add(
            new MedicationRemindersConfiguredEvent(
                PlanId.Value,
                OwnerId.Value,
                enabled,
                occurredAtUtc));
    }

    // -------------------------------------------------
    // Generación idempotente de dosis programadas
    // -------------------------------------------------

    public MedicationDose ScheduleDose(
        MedicationScheduleId scheduleId,
        DateOnly localDate,
        DateTime scheduledAtUtc,
        DateTime occurredAtUtc)
    {
        EnsureUtc(scheduledAtUtc);
        EnsureUtc(occurredAtUtc);
        EnsureActive();

        if (!IsActiveOn(localDate))
        {
            throw new MedicationDomainException(
                "Dose date is outside the active treatment period.");
        }

        var schedule = _schedules.SingleOrDefault(
            x => x.ScheduleId == scheduleId);

        if (schedule is null)
        {
            throw new MedicationDomainException(
                "Medication schedule not found.");
        }

        if (!schedule.AppliesOn(localDate))
        {
            throw new MedicationDomainException(
                "Medication is not scheduled for that day.");
        }

        // El par horario + versión + fecha identifica
        // una única ocurrencia dentro del agregado.
        var existing = _doses.SingleOrDefault(x =>
            x.ScheduleId == scheduleId &&
            x.ScheduleVersion == ScheduleVersion &&
            x.LocalDate == localDate);

        if (existing is not null)
        {
            if (existing.ScheduledAtUtc != scheduledAtUtc)
            {
                throw new MedicationDomainException(
                    "An existing dose has a different UTC instant.");
            }

            return existing;
        }

        var dose = MedicationDose.Create(
            PlanId,
            schedule,
            ScheduleVersion,
            localDate,
            scheduledAtUtc);

        _doses.Add(dose);
        UpdatedAtUtc = occurredAtUtc;

        _domainEvents.Add(
            new MedicationDoseScheduledEvent(
                PlanId.Value,
                dose.DoseId.Value,
                PetId.Value,
                scheduledAtUtc,
                occurredAtUtc));

        return dose;
    }

    // -------------------------------------------------
    // Policy: generar evento de recordatorio
    // -------------------------------------------------

    public void RecordReminderGenerated(
        MedicationDoseId doseId,
        DateOnly currentLocalDate,
        DateTime occurredAtUtc)
    {
        EnsureUtc(occurredAtUtc);

        if (!IsActiveOn(currentLocalDate))
        {
            throw new MedicationDomainException(
                "Medication plan is not active for reminders.");
        }

        if (!RemindersEnabled)
        {
            throw new MedicationDomainException(
                "Medication reminders are disabled.");
        }

        var dose = FindDoseOrThrow(doseId);

        if (dose.Status != MedicationDoseStatus.Pending)
        {
            throw new MedicationDomainException(
                "Only pending doses can generate reminders.");
        }

        _domainEvents.Add(
            new MedicationReminderGeneratedEvent(
                PlanId.Value,
                doseId.Value,
                OwnerId.Value,
                occurredAtUtc));

        UpdatedAtUtc = occurredAtUtc;

        // La deduplicación definitiva de recordatorios
        // corresponderá a Infrastructure mediante
        // una restricción única en MySQL.
    }

    // -------------------------------------------------
    // US18: registrar dosis administrada
    // -------------------------------------------------

    public DoseAdministration RegisterAdministration(
        MedicationDoseId doseId,
        MedicationOwnerId actingOwnerId,
        DateTime administeredAtUtc,
        DateTime recordedAtUtc,
        DateOnly currentLocalDate)
    {
        EnsureUtc(administeredAtUtc);
        EnsureUtc(recordedAtUtc);

        if (!IsOwnedBy(actingOwnerId))
        {
            throw new MedicationDomainException(
                "Only the pet owner can register administration.");
        }

        if (!IsActiveOn(currentLocalDate))
        {
            throw new MedicationDomainException(
                "Medication plan is not active.");
        }

        var dose = FindDoseOrThrow(doseId);

        var administration = dose.Administer(
            actingOwnerId,
            administeredAtUtc,
            recordedAtUtc);

        UpdatedAtUtc = recordedAtUtc;

        _domainEvents.Add(
            new MedicationDoseAdministeredEvent(
                Guid.NewGuid(),
                PlanId.Value,
                dose.DoseId.Value,
                administration.AdministrationId.Value,
                PetId.Value,
                OwnerId.Value,
                dose.ScheduledAtUtc,
                administeredAtUtc,
                recordedAtUtc));

        return administration;
    }

    // -------------------------------------------------
    // Cambio de horarios y versión de la prescripción
    // -------------------------------------------------

    public void ChangeSchedules(
        IReadOnlyCollection<MedicationSchedule> schedules,
        DateTime occurredAtUtc)
    {
        EnsureUtc(occurredAtUtc);
        EnsureActive();

        ArgumentNullException.ThrowIfNull(schedules);

        ValidateSchedules(schedules);

        // No se borran administraciones anteriores.
        // Las dosis pendientes antiguas se invalidan.
        foreach (var dose in _doses)
        {
            dose.CancelIfPending();
        }

        _schedules.Clear();
        _schedules.AddRange(schedules);

        ScheduleVersion++;

        if (_schedules.Count == 0)
        {
            RemindersEnabled = false;
        }

        UpdatedAtUtc = occurredAtUtc;

        _domainEvents.Add(
            new MedicationScheduleChangedEvent(
                PlanId.Value,
                ScheduleVersion,
                occurredAtUtc));
    }

    // -------------------------------------------------
    // Finalización y cancelación de tratamiento
    // -------------------------------------------------

    public void Complete(DateTime occurredAtUtc)
    {
        Close(
            MedicationPlanStatus.Completed,
            occurredAtUtc);
    }

    public void Cancel(DateTime occurredAtUtc)
    {
        Close(
            MedicationPlanStatus.Cancelled,
            occurredAtUtc);
    }

    private void Close(
        MedicationPlanStatus finalStatus,
        DateTime occurredAtUtc)
    {
        EnsureUtc(occurredAtUtc);
        EnsureActive();

        foreach (var dose in _doses)
        {
            dose.CancelIfPending();
        }

        Status = finalStatus;
        RemindersEnabled = false;
        UpdatedAtUtc = occurredAtUtc;

        _domainEvents.Add(
            new MedicationPlanClosedEvent(
                PlanId.Value,
                finalStatus.ToString(),
                occurredAtUtc));
    }

    private MedicationDose FindDoseOrThrow(
        MedicationDoseId doseId)
    {
        return _doses.SingleOrDefault(
                   x => x.DoseId == doseId)
               ?? throw new MedicationDomainException(
                   "Medication dose does not belong to this plan.");
    }

    // -------------------------------------------------
    // Rehidratación desde EF Core
    // -------------------------------------------------

    public static MedicationPlan Rehydrate(
        MedicationPlanId planId,
        MedicationOwnerId ownerId,
        MedicationPetId petId,
        MedicationClinicId clinicId,
        MedicationVeterinarianId veterinarianId,
        Guid? clinicalAttentionId,
        string medicationName,
        string route,
        string instructions,
        DateOnly startDate,
        DateOnly? endDate,
        string timeZoneId,
        MedicationPlanStatus status,
        bool remindersEnabled,
        int scheduleVersion,
        DateTime createdAtUtc,
        DateTime updatedAtUtc,
        IReadOnlyCollection<MedicationSchedule> schedules,
        IReadOnlyCollection<MedicationDose> doses)
    {
        ValidateIdentifiers(
            ownerId,
            petId,
            clinicId,
            veterinarianId);

        ValidatePrescription(
            medicationName,
            route,
            instructions,
            startDate,
            endDate,
            timeZoneId,
            createdAtUtc,
            clinicalAttentionId);

        EnsureUtc(updatedAtUtc);

        ArgumentNullException.ThrowIfNull(schedules);
        ArgumentNullException.ThrowIfNull(doses);

        if (planId.Value == Guid.Empty ||
            scheduleVersion < 1 ||
            !Enum.IsDefined(status) ||
            doses.Any(x =>
                x.PlanId != planId ||
                x.ScheduleVersion > scheduleVersion))
        {
            throw new MedicationDomainException(
                "Invalid persisted medication plan.");
        }

        ValidateSchedules(schedules);

        return new MedicationPlan(
            planId,
            ownerId,
            petId,
            clinicId,
            veterinarianId,
            clinicalAttentionId,
            medicationName.Trim(),
            route.Trim(),
            instructions?.Trim() ?? string.Empty,
            startDate,
            endDate,
            timeZoneId.Trim(),
            status,
            remindersEnabled,
            scheduleVersion,
            createdAtUtc,
            updatedAtUtc,
            schedules,
            doses);
    }

    // -------------------------------------------------
    // Eventos de dominio
    // -------------------------------------------------

    public IReadOnlyCollection<IDomainEvent> PullDomainEvents()
    {
        var events = _domainEvents.ToArray();
        _domainEvents.Clear();

        return events;
    }

    // -------------------------------------------------
    // Validaciones internas
    // -------------------------------------------------

    private static void ValidateIdentifiers(
        MedicationOwnerId ownerId,
        MedicationPetId petId,
        MedicationClinicId clinicId,
        MedicationVeterinarianId veterinarianId)
    {
        if (ownerId.Value == Guid.Empty ||
            petId.Value == Guid.Empty ||
            clinicId.Value == Guid.Empty ||
            veterinarianId.Value == Guid.Empty)
        {
            throw new MedicationDomainException(
                "Valid owner, pet, clinic and veterinarian IDs are required.");
        }
    }

    private static void ValidatePrescription(
        string medicationName,
        string route,
        string instructions,
        DateOnly startDate,
        DateOnly? endDate,
        string timeZoneId,
        DateTime occurredAtUtc,
        Guid? clinicalAttentionId)
    {
        EnsureUtc(occurredAtUtc);

        if (string.IsNullOrWhiteSpace(medicationName) ||
            medicationName.Trim().Length > 200)
        {
            throw new MedicationDomainException(
                "A valid medication name is required.");
        }

        if (string.IsNullOrWhiteSpace(route) ||
            route.Trim().Length > 100)
        {
            throw new MedicationDomainException(
                "A valid administration route is required.");
        }

        if ((instructions?.Length ?? 0) > 2000)
        {
            throw new MedicationDomainException(
                "Medication instructions are too long.");
        }

        if (endDate.HasValue &&
            endDate.Value < startDate)
        {
            throw new MedicationDomainException(
                "Medication end date cannot precede start date.");
        }

        if (string.IsNullOrWhiteSpace(timeZoneId) ||
            timeZoneId.Trim().Length > 100)
        {
            throw new MedicationDomainException(
                "A valid time zone is required.");
        }

        if (clinicalAttentionId == Guid.Empty)
        {
            throw new MedicationDomainException(
                "Clinical attention ID cannot be empty.");
        }
    }

    private static void ValidateSchedules(
        IReadOnlyCollection<MedicationSchedule> schedules)
    {
        if (schedules.Any(x => x is null))
        {
            throw new MedicationDomainException(
                "Medication schedules cannot contain null.");
        }

        var items = schedules.ToArray();

        if (items.Select(x => x.ScheduleId).Distinct().Count()
            != items.Length)
        {
            throw new MedicationDomainException(
                "Medication schedule IDs must be unique.");
        }

        for (var i = 0; i < items.Length; i++)
        {
            for (var j = i + 1; j < items.Length; j++)
            {
                if (items[i].ConflictsWith(items[j]))
                {
                    throw new MedicationDomainException(
                        "Medication schedules cannot overlap " +
                        "at the same local time.");
                }
            }
        }
    }
}
