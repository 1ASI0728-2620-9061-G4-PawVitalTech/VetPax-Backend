
using VetPax.Application.Modules.AppointmentManagement.Models;
using VetPax.Application.Modules.AppointmentManagement.Ports;
using VetPax.Application.Modules.AppointmentManagement.Services;
using VetPax.Domain.Modules.AppointmentManagement.Repositories;
using VetPax.Domain.Modules.AppointmentManagement.ValueObjects;

namespace VetPax.Application.Modules.AppointmentManagement.UseCases.GetVeterinarianAgenda;

public sealed record GetVeterinarianAgendaQuery(
    Guid ClinicId,
    DateOnly From,
    DateOnly To);

public sealed class GetVeterinarianAgendaHandler
{
    private readonly IAppointmentRepository _appointments;
    private readonly IAppointmentPatientPort _patients;
    private readonly AppointmentAccessService _access;

    public GetVeterinarianAgendaHandler(
        IAppointmentRepository appointments,
        IAppointmentPatientPort patients,
        AppointmentAccessService access)
    {
        _appointments = appointments;
        _patients = patients;
        _access = access;
    }

    public async Task<IReadOnlyList<AgendaAppointmentData>> HandleAsync(
        GetVeterinarianAgendaQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.ClinicId == Guid.Empty ||
            query.From == default ||
            query.To == default)
        {
            throw new ArgumentException(
                "Valid clinic and date range are required.");
        }

        if (query.To < query.From)
        {
            throw new ArgumentException(
                "End date cannot precede start date.");
        }

        if (query.To.DayNumber - query.From.DayNumber > 30)
        {
            throw new ArgumentException(
                "Agenda query cannot exceed 31 days.");
        }

        var veterinarianId =
            await _access.RequireVeterinarianAsync(
                cancellationToken);

        var clinicId = new AppointmentClinicId(query.ClinicId);

        await _access.EnsureActiveVeterinarianAsync(
            veterinarianId,
            clinicId,
            cancellationToken);

        var entries = await _appointments.FindAgendaAsync(
            veterinarianId,
            query.From,
            query.To,
            cancellationToken);

        // Defensa adicional: solamente citas de la clínica solicitada.
        var authorizedEntries = entries
            .Where(e =>
                e.ClinicId == clinicId &&
                e.VeterinarianId == veterinarianId)
            .ToArray();

        var petIds = authorizedEntries
            .Select(e => e.PetId.Value)
            .Distinct()
            .ToArray();

        var summaries = petIds.Length == 0
            ? new Dictionary<Guid, AppointmentPatientSummary>()
            : await _patients.GetSummariesAsync(
                petIds,
                cancellationToken);

        return authorizedEntries
            .OrderBy(e => e.Slot.Date)
            .ThenBy(e => e.Slot.StartTime)
            .Select(e =>
            {
                summaries.TryGetValue(
                    e.PetId.Value,
                    out var patient);

                return AgendaAppointmentData.From(
                    e,
                    patient);
            })
            .ToArray();
    }
}
