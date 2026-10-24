using VetPax.Domain.Modules.AppointmentManagement.Aggregates;
using VetPax.Domain.Shared.Events;

namespace VetPax.Application.Modules.AppointmentManagement.Ports;

public enum AppointmentNotificationAction
{
    Schedule,
    Reschedule,
    Cancel,
    Attended
}

public interface IAppointmentNotificationPort
{
    Task StageChangesAsync(
        Appointment appointment,
        AppointmentNotificationAction action,
        IReadOnlyCollection<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default);
}