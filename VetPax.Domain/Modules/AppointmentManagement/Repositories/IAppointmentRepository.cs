using VetPax.Domain.Modules.AppointmentManagement.Aggregates;
using VetPax.Domain.Modules.AppointmentManagement.ReadModels;
using VetPax.Domain.Modules.AppointmentManagement.ValueObjects;

namespace VetPax.Domain.Modules.AppointmentManagement.Repositories;

public interface IAppointmentRepository
{
    Task<Appointment?> FindByIdAsync(
        AppointmentId appointmentId,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        Appointment appointment,
        CancellationToken cancellationToken = default);

    Task<bool> HasOverlapAsync(
        AppointmentVeterinarianId veterinarianId,
        AppointmentSlot slot,
        AppointmentId? excludeAppointmentId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AgendaAppointmentEntry>> FindAgendaAsync(
        AppointmentVeterinarianId veterinarianId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default);
}