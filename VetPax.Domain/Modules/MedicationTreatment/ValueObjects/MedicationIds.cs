namespace VetPax.Domain.Modules.MedicationTreatment.ValueObjects;

public readonly record struct MedicationPlanId(Guid Value)
{
    public static MedicationPlanId New() => new(Guid.NewGuid());
}

public readonly record struct MedicationDoseId(Guid Value)
{
    public static MedicationDoseId New() => new(Guid.NewGuid());
}

public readonly record struct MedicationScheduleId(Guid Value)
{
    public static MedicationScheduleId New() => new(Guid.NewGuid());
}

public readonly record struct MedicationAdministrationId(Guid Value)
{
    public static MedicationAdministrationId New() => new(Guid.NewGuid());
}

public readonly record struct MedicationOwnerId(Guid Value);

public readonly record struct MedicationPetId(Guid Value);

public readonly record struct MedicationClinicId(Guid Value);

public readonly record struct MedicationVeterinarianId(Guid Value);