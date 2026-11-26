
using VetPax.Application.Common.Ports;
using VetPax.Application.Modules.AppointmentManagement.Models;
using VetPax.Application.Modules.AppointmentManagement.Ports;
using VetPax.Application.Modules.AppointmentManagement.Services;
using VetPax.Domain.Modules.AppointmentManagement.Repositories;
using VetPax.Domain.Modules.AppointmentManagement.ValueObjects;
using VetPax.Domain.Modules.IdentityAccessManagement.ValueObjects;

namespace VetPax.Application.Modules.AppointmentManagement.UseCases.CancelAppointment;

public sealed record CancelAppointmentCommand(
    Guid AppointmentId);

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

        // 1. Obtener usuario autenticado.
        var identity =
            await _access.RequireOwnerOrVeterinarianAsync(
                cancellationToken);

        // 2. Buscar cita.
        var appointment = await _appointments.FindByIdAsync(
            new AppointmentId(command.AppointmentId),
            cancellationToken)
            ?? throw new KeyNotFoundException(
                "Appointment not found.");

        // 3. Autorizar según rol.
        if (identity.Role == UserRole.Owner)
        {
            var ownerId = new AppointmentOwnerId(
                identity.UserId.Value);

            // El propietario solo cancela sus citas.
            AppointmentAccessService.EnsureOwnedBy(
                appointment,
                ownerId);
        }
        else if (identity.Role == UserRole.Veterinarian)
        {
            var veterinarianId =
                new AppointmentVeterinarianId(
                    identity.UserId.Value);

            // Solo puede cancelar sus citas asignadas.
            if (!appointment.IsAssignedTo(veterinarianId))
            {
                throw new UnauthorizedAccessException(
                    "Appointment is not assigned to this veterinarian.");
            }

            // Debe estar activo en esa clínica.
            await _access.EnsureActiveVeterinarianAsync(
                veterinarianId,
                appointment.ClinicId,
                cancellationToken);
        }
        else
        {
            throw new UnauthorizedAccessException(
                "Role is not authorized.");
        }

        // 4. El dominio permite cancelar solo citas pendientes.
        appointment.Cancel(_access.UtcNow);

        // 5. Persistir cambios.
        await _appointments.SaveAsync(
            appointment,
            cancellationToken);

        // 6. Registrar evento y notificaciones.
        await _notifications.StageChangesAsync(
            appointment,
            AppointmentNotificationAction.Cancel,
            appointment.PullDomainEvents(),
            cancellationToken);

        await _unitOfWork.CommitAsync(cancellationToken);

        return AppointmentData.From(appointment);
    }
}
