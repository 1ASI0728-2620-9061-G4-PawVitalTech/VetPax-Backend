
using VetPax.Application.Common.Ports;
using VetPax.Application.Modules.AppointmentManagement.Exceptions;
using VetPax.Application.Modules.AppointmentManagement.Models;
using VetPax.Application.Modules.AppointmentManagement.Ports;
using VetPax.Application.Modules.AppointmentManagement.Services;
using VetPax.Domain.Modules.AppointmentManagement.Aggregates;
using VetPax.Domain.Modules.AppointmentManagement.Repositories;
using VetPax.Domain.Modules.AppointmentManagement.ValueObjects;

namespace VetPax.Application.Modules.AppointmentManagement.UseCases.ScheduleAppointment;

public sealed record ScheduleAppointmentCommand(
    Guid PetId,
    Guid ClinicId,
    Guid VeterinarianId,
    DateOnly Date,
    TimeOnly StartTime,
    string Reason);

public sealed class ScheduleAppointmentHandler
{
    private readonly IAppointmentRepository _appointments;
    private readonly AppointmentAccessService _access;
    private readonly IAppointmentNotificationPort _notifications;
    private readonly IUnitOfWork _unitOfWork;

    public ScheduleAppointmentHandler(
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
        ScheduleAppointmentCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.PetId == Guid.Empty ||
            command.ClinicId == Guid.Empty ||
            command.VeterinarianId == Guid.Empty)
        {
            throw new ArgumentException(
                "Valid pet, clinic and veterinarian IDs are required.");
        }

        var ownerId = await _access.RequireOwnerAsync(
            cancellationToken);

        var petId = new AppointmentPetId(command.PetId);
        var clinicId = new AppointmentClinicId(command.ClinicId);
        var veterinarianId =
            new AppointmentVeterinarianId(command.VeterinarianId);

        var slot = new AppointmentSlot(
            command.Date,
            command.StartTime);

        await _access.EnsureCanBookAsync(
            ownerId,
            petId,
            clinicId,
            veterinarianId,
            slot,
            cancellationToken);

        var occupied = await _appointments.HasOverlapAsync(
            veterinarianId,
            slot,
            null,
            cancellationToken);

        if (occupied)
        {
            throw new AppointmentConflictException(
                "The selected appointment slot is unavailable.");
        }

        var appointment = Appointment.Schedule(
            ownerId,
            petId,
            clinicId,
            veterinarianId,
            slot,
            command.Reason,
            _access.UtcNow);

        await _appointments.SaveAsync(
            appointment,
            cancellationToken);

        await _notifications.StageChangesAsync(
            appointment,
            AppointmentNotificationAction.Schedule,
            appointment.PullDomainEvents(),
            cancellationToken);

        await _unitOfWork.CommitAsync(cancellationToken);

        return AppointmentData.From(appointment);
    }
}
