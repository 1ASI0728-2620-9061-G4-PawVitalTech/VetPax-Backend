
using VetPax.Application.Common.Ports;
using VetPax.Application.Modules.AppointmentManagement.Exceptions;
using VetPax.Application.Modules.AppointmentManagement.Models;
using VetPax.Application.Modules.AppointmentManagement.Ports;
using VetPax.Application.Modules.AppointmentManagement.Services;
using VetPax.Domain.Modules.AppointmentManagement.Aggregates;
using VetPax.Domain.Modules.AppointmentManagement.Repositories;
using VetPax.Domain.Modules.AppointmentManagement.ValueObjects;
using VetPax.Domain.Modules.IdentityAccessManagement.ValueObjects;

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

        // 1. Identificar al usuario autenticado.
        var identity =
            await _access.RequireOwnerOrVeterinarianAsync(
                cancellationToken);

        var petId = new AppointmentPetId(command.PetId);
        var clinicId = new AppointmentClinicId(command.ClinicId);
        var veterinarianId =
            new AppointmentVeterinarianId(command.VeterinarianId);

        AppointmentOwnerId ownerId;

        // 2. Resolver permisos según el rol.
        if (identity.Role == UserRole.Owner)
        {
            // El propietario puede agendar para sus mascotas.
            ownerId = new AppointmentOwnerId(
                identity.UserId.Value);
        }
        else if (identity.Role == UserRole.Veterinarian)
        {
            // El veterinario solo puede agendar para sí mismo.
            var currentVeterinarianId =
                new AppointmentVeterinarianId(
                    identity.UserId.Value);

            if (currentVeterinarianId != veterinarianId)
            {
                throw new UnauthorizedAccessException(
                    "Cannot schedule for another veterinarian.");
            }

            // Debe pertenecer activamente a la clínica.
            await _access.EnsureActiveVeterinarianAsync(
                currentVeterinarianId,
                clinicId,
                cancellationToken);

            // Buscar propietario real del paciente.
            ownerId = await _access.RequireLinkedPatientOwnerAsync(
                petId,
                clinicId,
                cancellationToken);
        }
        else
        {
            throw new UnauthorizedAccessException(
                "Role is not authorized.");
        }

        // 3. Construir el horario.
        var slot = new AppointmentSlot(
            command.Date,
            command.StartTime);

        // 4. Validar fecha futura, vinculación,
        // veterinario activo y horario de clínica.
        await _access.EnsureCanBookAsync(
            ownerId,
            petId,
            clinicId,
            veterinarianId,
            slot,
            cancellationToken);

        // 5. Comprobar que no haya otra cita en ese horario.
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

        // 6. Crear la cita.
        var appointment = Appointment.Schedule(
            ownerId,
            petId,
            clinicId,
            veterinarianId,
            slot,
            command.Reason,
            _access.UtcNow);

        // 7. Persistir cambios.
        await _appointments.SaveAsync(
            appointment,
            cancellationToken);

        // 8. Registrar eventos y notificaciones.
        await _notifications.StageChangesAsync(
            appointment,
            AppointmentNotificationAction.Schedule,
            appointment.PullDomainEvents(),
            cancellationToken);

        await _unitOfWork.CommitAsync(cancellationToken);

        return AppointmentData.From(appointment);
    }
}
