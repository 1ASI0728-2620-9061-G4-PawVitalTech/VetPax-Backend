namespace VetPax.Domain.Modules.AppointmentManagement.ValueObjects;

public readonly record struct AppointmentId(Guid Value)
{
    public static AppointmentId New() => new(Guid.NewGuid());
}

public readonly record struct AppointmentOwnerId(Guid Value);

public readonly record struct AppointmentPetId(Guid Value);

public readonly record struct AppointmentClinicId(Guid Value);

public readonly record struct AppointmentVeterinarianId(Guid Value);