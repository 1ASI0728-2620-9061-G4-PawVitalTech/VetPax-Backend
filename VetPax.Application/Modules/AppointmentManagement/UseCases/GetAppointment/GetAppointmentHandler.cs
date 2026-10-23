
using VetPax.Application.Modules.AppointmentManagement.Models;
using VetPax.Application.Modules.AppointmentManagement.Services;
using VetPax.Application.Modules.IdentityAccessManagement.Ports;
using VetPax.Domain.Modules.AppointmentManagement.Repositories;
using VetPax.Domain.Modules.AppointmentManagement.ValueObjects;
using VetPax.Domain.Modules.IdentityAccessManagement.ValueObjects;

namespace VetPax.Application.Modules.AppointmentManagement.UseCases.GetAppointment;

public sealed record GetAppointmentQuery(Guid AppointmentId);

public sealed class GetAppointmentHandler
{
    private readonly IAppointmentRepository _appointments;
    private readonly IAuthenticatedUserPort _currentUser;
    private readonly AppointmentAccessService _access;

    public GetAppointmentHandler(
        IAppointmentRepository appointments,
        IAuthenticatedUserPort currentUser,
        AppointmentAccessService access)
    {
        _appointments = appointments;
        _currentUser = currentUser;
        _access = access;
    }

    public async Task<AppointmentData> HandleAsync(
        GetAppointmentQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.AppointmentId == Guid.Empty)
        {
            throw new ArgumentException(
                "A valid appointment ID is required.");
        }

        var identity = await _currentUser.GetCurrentAsync(
            cancellationToken);

        if (identity is null ||
            identity.UserId.Value == Guid.Empty)
        {
            throw new UnauthorizedAccessException(
                "Authentication required.");
        }

        var appointment = await _appointments.FindByIdAsync(
            new AppointmentId(query.AppointmentId),
            cancellationToken)
            ?? throw new KeyNotFoundException(
                "Appointment not found.");

        if (identity.Role == UserRole.Owner)
        {
            AppointmentAccessService.EnsureOwnedBy(
                appointment,
                new AppointmentOwnerId(identity.UserId.Value));
        }
        else if (identity.Role == UserRole.Veterinarian)
        {
            var veterinarianId =
                new AppointmentVeterinarianId(identity.UserId.Value);

            if (!appointment.IsAssignedTo(veterinarianId))
            {
                throw new UnauthorizedAccessException(
                    "Appointment is not assigned to this veterinarian.");
            }

            await _access.EnsureActiveVeterinarianAsync(
                veterinarianId,
                appointment.ClinicId,
                cancellationToken);
        }
        else
        {
            throw new UnauthorizedAccessException(
                "Role is not authorized to view appointments.");
        }

        return AppointmentData.From(appointment);
    }
}
