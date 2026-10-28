using VetPax.Infrastructure.Modules.AppointmentManagement.Persistence;

namespace VetPax.Infrastructure.Modules.AppointmentManagement.Processing;

public interface IAppointmentReminderSender
{
    Task SendAsync(
        AppointmentRow appointment,
        CancellationToken cancellationToken = default);
}