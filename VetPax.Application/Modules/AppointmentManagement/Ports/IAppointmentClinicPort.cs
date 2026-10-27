using VetPax.Domain.Modules.AppointmentManagement.ValueObjects;

namespace VetPax.Application.Modules.AppointmentManagement.Ports;

public sealed record AppointmentOpeningInterval(
    TimeOnly OpenTime,
    TimeOnly CloseTime);

public interface IAppointmentClinicPort
{
    Task<bool> IsActiveVeterinarianAsync(
        Guid veterinarianId,
        Guid clinicId,
        CancellationToken cancellationToken = default);

    Task<bool> IsWithinOpeningHoursAsync(
        Guid clinicId,
        AppointmentSlot slot,
        CancellationToken cancellationToken = default);
    
    Task<IReadOnlyList<AppointmentOpeningInterval>>
        GetOpeningIntervalsAsync(
            Guid clinicId,
            DayOfWeek dayOfWeek,
            CancellationToken cancellationToken = default);
}