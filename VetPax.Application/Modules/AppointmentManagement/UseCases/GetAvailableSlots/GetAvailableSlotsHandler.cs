using VetPax.Application.Modules.AppointmentManagement.Ports;
using VetPax.Application.Modules.AppointmentManagement.Services;
using VetPax.Domain.Modules.AppointmentManagement.Enums;
using VetPax.Domain.Modules.AppointmentManagement.Repositories;
using VetPax.Domain.Modules.AppointmentManagement.ValueObjects;

namespace VetPax.Application.Modules.AppointmentManagement.UseCases.GetAvailableSlots;

public sealed record GetAvailableSlotsQuery(
    Guid ClinicId,
    Guid VeterinarianId,
    DateOnly Date);

public sealed record AvailableSlotData(
    TimeOnly StartTime,
    TimeOnly EndTime);

public sealed record AvailableSlotsData(
    Guid ClinicId,
    Guid VeterinarianId,
    DateOnly Date,
    string TimeZoneId,
    int DurationMinutes,
    IReadOnlyList<AvailableSlotData> Slots);

public sealed class GetAvailableSlotsHandler(
    IAppointmentRepository appointments,
    IAppointmentClinicPort clinics,
    AppointmentAccessService access)
{
    public async Task<AvailableSlotsData> HandleAsync(
        GetAvailableSlotsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.ClinicId == Guid.Empty ||
            query.VeterinarianId == Guid.Empty ||
            query.Date == default)
        {
            throw new ArgumentException(
                "clinicId, veterinarianId and date are required.");
        }

        // Solo propietarios pueden consultar la disponibilidad.
        await access.RequireOwnerAsync(cancellationToken);

        var veterinarianId =
            new AppointmentVeterinarianId(query.VeterinarianId);

        var clinicId =
            new AppointmentClinicId(query.ClinicId);

        // Comprobar que el veterinario esté vinculado a la clínica.
        await access.EnsureActiveVeterinarianAsync(
            veterinarianId,
            clinicId,
            cancellationToken);

        // Obtener los horarios de apertura de la clínica.
        var intervals = await clinics.GetOpeningIntervalsAsync(
            query.ClinicId,
            query.Date.DayOfWeek,
            cancellationToken);

        // Buscar citas del veterinario en todas sus clínicas.
        var appointmentsForDay = await appointments.FindAgendaAsync(
            veterinarianId,
            query.Date,
            query.Date,
            cancellationToken);

        // Las citas canceladas no bloquean horarios.
        var occupied = appointmentsForDay
            .Where(a => a.Status != AppointmentStatus.Cancelled)
            .Select(a => a.Slot)
            .ToArray();

        // Hora local de Lima, coherente con AppointmentAccessService.
        var localNow = access.UtcNow.AddHours(-5);

        var durationTicks = TimeSpan
            .FromMinutes(AppointmentSlot.DurationMinutes)
            .Ticks;

        var available = new List<AvailableSlotData>();

        foreach (var interval in intervals)
        {
            // Alinear el inicio a bloques de 30 minutos.
            var first =
                ((interval.OpenTime.Ticks + durationTicks - 1)
                 / durationTicks) * durationTicks;

            for (var tick = first;
                 tick + durationTicks <= interval.CloseTime.Ticks;
                 tick += durationTicks)
            {
                var start = new TimeOnly(tick);

                var slot = new AppointmentSlot(
                    query.Date,
                    start);

                // No ofrecer citas en el pasado.
                if (query.Date.ToDateTime(start) <= localNow)
                    continue;

                // No ofrecer horarios ocupados.
                if (occupied.Any(existing =>
                        existing.OverlapsWith(slot)))
                    continue;

                available.Add(new AvailableSlotData(
                    slot.StartTime,
                    slot.EndTime));
            }
        }

        return new AvailableSlotsData(
            query.ClinicId,
            query.VeterinarianId,
            query.Date,
            "America/Lima",
            AppointmentSlot.DurationMinutes,
            available
                .OrderBy(s => s.StartTime)
                .ToArray());
    }
}