
using VetPax.Application.Common.Ports;
using VetPax.Application.Modules.AppointmentManagement.Exceptions;
using VetPax.Application.Modules.AppointmentManagement.Models;
using VetPax.Application.Modules.AppointmentManagement.Ports;
using VetPax.Application.Modules.AppointmentManagement.Services;
using VetPax.Application.Modules.IdentityAccessManagement.Ports;
using VetPax.Domain.Modules.AppointmentManagement.Exceptions;
using VetPax.Domain.Modules.AppointmentManagement.Repositories;
using VetPax.Domain.Modules.AppointmentManagement.ValueObjects;
using VetPax.Domain.Modules.IdentityAccessManagement.ValueObjects;

namespace VetPax.Application.Modules.AppointmentManagement.UseCases.RescheduleAppointment;

public sealed record RescheduleAppointmentCommand(
    Guid AppointmentId,
    DateOnly Date,
    TimeOnly StartTime);

public sealed class RescheduleAppointmentHandler
{
    private readonly IAppointmentRepository _appointments;
    private readonly AppointmentAccessService _access;
    private readonly IAuthenticatedUserPort _currentUser;
    private readonly IAppointmentNotificationPort _notifications;
    private readonly IUnitOfWork _unitOfWork;

    public RescheduleAppointmentHandler(
        IAppointmentRepository appointments,
        AppointmentAccessService access,
        IAuthenticatedUserPort currentUser,
        IAppointmentNotificationPort notifications,
        IUnitOfWork unitOfWork)
    {
        _appointments = appointments;
        _access = access;
        _currentUser = currentUser;
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

        // 1. Obtener usuario autenticado.
        var identity = await _currentUser.GetCurrentAsync(
            cancellationToken);

        if (identity is null ||
            identity.UserId.Value == Guid.Empty ||
            (identity.Role != UserRole.Owner &&
             identity.Role != UserRole.Veterinarian))
        {
            throw new UnauthorizedAccessException(
                "Authenticated owner or veterinarian required.");
        }

        // 2. Buscar la cita.
        var appointmentId = new AppointmentId(
            command.AppointmentId);

        var appointment = await _appointments.FindByIdAsync(
            appointmentId,
            cancellationToken)
            ?? throw new KeyNotFoundException(
                "Appointment not found.");

        // 3. Autorizar según el rol.
        if (identity.Role == UserRole.Owner)
        {
            // El propietario solo puede modificar sus citas.
            var ownerId = new AppointmentOwnerId(
                identity.UserId.Value);

            AppointmentAccessService.EnsureOwnedBy(
                appointment,
                ownerId);
        }
        else if (identity.Role == UserRole.Veterinarian)
        {
            var veterinarianId =
                new AppointmentVeterinarianId(
                    identity.UserId.Value);

            // La cita debe estar asignada al veterinario.
            if (!appointment.IsAssignedTo(veterinarianId))
            {
                throw new UnauthorizedAccessException(
                    "Appointment is not assigned to this veterinarian.");
            }

            // Debe pertenecer activamente a la clínica.
            await _access.EnsureActiveVeterinarianAsync(
                veterinarianId,
                appointment.ClinicId,
                cancellationToken);
        }

        // 4. Solo se reprograman citas pendientes.
        if (!appointment.IsPending())
        {
            throw new AppointmentStateException(
                "Only pending appointments can be rescheduled.");
        }

        // 5. Construir el nuevo horario.
        var newSlot = new AppointmentSlot(
            command.Date,
            command.StartTime);

        if (appointment.Slot == newSlot)
        {
            throw new AppointmentStateException(
                "The new schedule must be different.");
        }

        // 6. Verificar fecha futura, mascota vinculada,
        // veterinario activo y horarios de la clínica.
        await _access.EnsureCanBookAsync(
            appointment.OwnerId,
            appointment.PetId,
            appointment.ClinicId,
            appointment.VeterinarianId,
            newSlot,
            cancellationToken);

        // 7. Evitar conflictos con otras citas.
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

        // 8. Actualizar la cita.
        appointment.Reschedule(
            newSlot,
            _access.UtcNow);

        await _appointments.SaveAsync(
            appointment,
            cancellationToken);

        // 9. Registrar eventos y notificaciones.
        await _notifications.StageChangesAsync(
            appointment,
            AppointmentNotificationAction.Reschedule,
            appointment.PullDomainEvents(),
            cancellationToken);

        await _unitOfWork.CommitAsync(cancellationToken);

        return AppointmentData.From(appointment);
    }
}
