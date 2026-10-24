
using VetPax.Application.Common.Ports;
using VetPax.Application.Modules.AppointmentManagement.Models;
using VetPax.Application.Modules.AppointmentManagement.Ports;
using VetPax.Application.Modules.AppointmentManagement.Services;
using VetPax.Domain.Modules.AppointmentManagement.Repositories;
using VetPax.Domain.Modules.AppointmentManagement.ValueObjects;

namespace VetPax.Application.Modules.AppointmentManagement.UseCases.CancelAppointment;

public sealed record CancelAppointmentCommand(Guid AppointmentId);

public sealed class CancelAppointmentHandler
{
    private readonly IAppointmentRepository _appointments;
    private readonly AppointmentAccessService _access;
    private readonly IAppointmentNotificationPort _notifications;
    private readonly IUnitOfWork _unitOfWork;

    public CancelAppointmentHandler(
        IAppointmentRepository appointments,
        AppointmentAccessService access,
        IAppointmentNotificationPort notifications,
        IUnitOfWork unitOfWork)
    {
        _appointments = appointments;
        _access = access;
        _notifications = notifications;
        _unitOfWork = unitOfWork;
    }

    public async Task<AppointmentData> HandleAsync(
        CancelAppointmentCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.AppointmentId == Guid.Empty)
        {
            throw new ArgumentException(
                "A valid appointment ID is required.");
        }

        var ownerId = await _access.RequireOwnerAsync(
            cancellationToken);

        var appointment = await _appointments.FindByIdAsync(
            new AppointmentId(command.AppointmentId),
            cancellationToken)
            ?? throw new KeyNotFoundException(
                "Appointment not found.");

        AppointmentAccessService.EnsureOwnedBy(
            appointment,
            ownerId);

        appointment.Cancel(_access.UtcNow);

        await _appointments.SaveAsync(
            appointment,
            cancellationToken);

        await _notifications.StageChangesAsync(
            appointment,
            AppointmentNotificationAction.Cancel,
            appointment.PullDomainEvents(),
            cancellationToken);

        await _unitOfWork.CommitAsync(cancellationToken);

        return AppointmentData.From(appointment);
    }
}
