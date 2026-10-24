
using VetPax.Application.Common.Ports;
using VetPax.Application.Modules.AppointmentManagement.Exceptions;
using VetPax.Application.Modules.AppointmentManagement.Models;
using VetPax.Application.Modules.AppointmentManagement.Ports;
using VetPax.Application.Modules.AppointmentManagement.Services;
using VetPax.Domain.Modules.AppointmentManagement.Exceptions;
using VetPax.Domain.Modules.AppointmentManagement.Repositories;
using VetPax.Domain.Modules.AppointmentManagement.ValueObjects;

namespace VetPax.Application.Modules.AppointmentManagement.UseCases.RescheduleAppointment;

public sealed record RescheduleAppointmentCommand(
    Guid AppointmentId,
    DateOnly Date,
    TimeOnly StartTime);

public sealed class RescheduleAppointmentHandler
{
    private readonly IAppointmentRepository _appointments;
    private readonly AppointmentAccessService _access;
    private readonly IAppointmentNotificationPort _notifications;
    private readonly IUnitOfWork _unitOfWork;

    public RescheduleAppointmentHandler(
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
        RescheduleAppointmentCommand command,
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

        var appointmentId = new AppointmentId(
            command.AppointmentId);

        var appointment = await _appointments.FindByIdAsync(
            appointmentId,
            cancellationToken)
            ?? throw new KeyNotFoundException(
                "Appointment not found.");

        AppointmentAccessService.EnsureOwnedBy(
            appointment,
            ownerId);

        if (!appointment.IsPending())
        {
            throw new AppointmentStateException(
                "Only pending appointments can be rescheduled.");
        }

        var newSlot = new AppointmentSlot(
            command.Date,
            command.StartTime);

        if (appointment.Slot == newSlot)
        {
            throw new AppointmentStateException(
                "The new schedule must be different.");
        }

        await _access.EnsureCanBookAsync(
            appointment.OwnerId,
            appointment.PetId,
            appointment.ClinicId,
            appointment.VeterinarianId,
            newSlot,
            cancellationToken);

        var occupied = await _appointments.HasOverlapAsync(
            appointment.VeterinarianId,
            newSlot,
            appointment.AppointmentId,
            cancellationToken);

        if (occupied)
        {
            throw new AppointmentConflictException(
                "The new appointment slot is unavailable.");
        }

        appointment.Reschedule(
            newSlot,
            _access.UtcNow);

        await _appointments.SaveAsync(
            appointment,
            cancellationToken);

        await _notifications.StageChangesAsync(
            appointment,
            AppointmentNotificationAction.Reschedule,
            appointment.PullDomainEvents(),
            cancellationToken);

        await _unitOfWork.CommitAsync(cancellationToken);

        return AppointmentData.From(appointment);
    }
}
