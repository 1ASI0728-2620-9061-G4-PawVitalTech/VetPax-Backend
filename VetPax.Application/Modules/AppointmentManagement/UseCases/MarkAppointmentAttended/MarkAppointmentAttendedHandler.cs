
using VetPax.Application.Common.Ports;
using VetPax.Application.Modules.AppointmentManagement.Models;
using VetPax.Application.Modules.AppointmentManagement.Ports;
using VetPax.Application.Modules.AppointmentManagement.Services;
using VetPax.Domain.Modules.AppointmentManagement.Exceptions;
using VetPax.Domain.Modules.AppointmentManagement.Repositories;
using VetPax.Domain.Modules.AppointmentManagement.ValueObjects;

namespace VetPax.Application.Modules.AppointmentManagement.UseCases.MarkAppointmentAttended;

public sealed record MarkAppointmentAttendedCommand(
    Guid AppointmentId,
    Guid ClinicalAttentionId);

public sealed class MarkAppointmentAttendedHandler
{
    private readonly IAppointmentRepository _appointments;
    private readonly AppointmentAccessService _access;
    private readonly IAppointmentClinicalAttentionPort _clinicalAttentions;
    private readonly IAppointmentNotificationPort _notifications;
    private readonly IUnitOfWork _unitOfWork;

    public MarkAppointmentAttendedHandler(
        IAppointmentRepository appointments,
        AppointmentAccessService access,
        IAppointmentClinicalAttentionPort clinicalAttentions,
        IAppointmentNotificationPort notifications,
        IUnitOfWork unitOfWork)
    {
        _appointments = appointments;
        _access = access;
        _clinicalAttentions = clinicalAttentions;
        _notifications = notifications;
        _unitOfWork = unitOfWork;
    }

    public async Task<AppointmentData> HandleAsync(
        MarkAppointmentAttendedCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.AppointmentId == Guid.Empty ||
            command.ClinicalAttentionId == Guid.Empty)
        {
            throw new ArgumentException(
                "Valid appointment and clinical attention IDs are required.");
        }

        var veterinarianId =
            await _access.RequireVeterinarianAsync(
                cancellationToken);

        var appointment = await _appointments.FindByIdAsync(
            new AppointmentId(command.AppointmentId),
            cancellationToken)
            ?? throw new KeyNotFoundException(
                "Appointment not found.");

        if (!appointment.IsAssignedTo(veterinarianId))
        {
            throw new UnauthorizedAccessException(
                "Only the assigned veterinarian can attend.");
        }

        await _access.EnsureActiveVeterinarianAsync(
            veterinarianId,
            appointment.ClinicId,
            cancellationToken);

        if (!appointment.IsPending())
        {
            throw new AppointmentStateException(
                "Only pending appointments can be attended.");
        }

        var linked = await _clinicalAttentions
            .IsLinkedToAppointmentAsync(
                command.ClinicalAttentionId,
                appointment.AppointmentId.Value,
                appointment.PetId.Value,
                appointment.ClinicId.Value,
                veterinarianId.Value,
                cancellationToken);

        if (!linked)
        {
            throw new UnauthorizedAccessException(
                "Clinical attention is not linked to this appointment.");
        }

        appointment.MarkAttended(
            command.ClinicalAttentionId,
            veterinarianId,
            _access.UtcNow);

        await _appointments.SaveAsync(
            appointment,
            cancellationToken);

        await _notifications.StageChangesAsync(
            appointment,
            AppointmentNotificationAction.Attended,
            appointment.PullDomainEvents(),
            cancellationToken);

        await _unitOfWork.CommitAsync(cancellationToken);

        return AppointmentData.From(appointment);
    }
}
