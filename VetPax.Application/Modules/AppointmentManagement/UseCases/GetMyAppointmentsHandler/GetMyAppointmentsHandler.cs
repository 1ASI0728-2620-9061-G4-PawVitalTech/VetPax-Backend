using VetPax.Application.Modules.AppointmentManagement.Models;
using VetPax.Application.Modules.AppointmentManagement.Services;
using VetPax.Domain.Modules.AppointmentManagement.Repositories;

namespace VetPax.Application.Modules.AppointmentManagement
    .UseCases.GetMyAppointments;

public sealed record GetMyAppointmentsQuery();

public sealed class GetMyAppointmentsHandler
{
    private readonly IAppointmentRepository _appointments;
    private readonly AppointmentAccessService _access;

    public GetMyAppointmentsHandler(
        IAppointmentRepository appointments,
        AppointmentAccessService access)
    {
        _appointments = appointments;
        _access = access;
    }

    public async Task<IReadOnlyList<AppointmentData>> HandleAsync(
        GetMyAppointmentsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var ownerId = await _access.RequireOwnerAsync(
            cancellationToken);

        var appointments = await _appointments.FindByOwnerAsync(
            ownerId,
            cancellationToken);

        return appointments
            .Select(AppointmentData.From)
            .ToArray();
    }
}