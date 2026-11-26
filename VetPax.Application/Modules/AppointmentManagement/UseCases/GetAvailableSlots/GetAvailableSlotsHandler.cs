
using VetPax.Application.Modules.AppointmentManagement.Ports;
using VetPax.Application.Modules.AppointmentManagement.Services;
using VetPax.Domain.Modules.AppointmentManagement.Enums;
using VetPax.Domain.Modules.AppointmentManagement.Repositories;
using VetPax.Domain.Modules.AppointmentManagement.ValueObjects;
using VetPax.Domain.Modules.IdentityAccessManagement.ValueObjects;

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

        // 1. Permitir propietario o veterinario.
        var identity =
            await access.RequireOwnerOrVeterinarianAsync(
                cancellationToken);

        var veterinarianId =
            new AppointmentVeterinarianId(query.VeterinarianId);

        var clinicId =
            new AppointmentClinicId(query.ClinicId);

        // 2. El veterinario consulta su propia disponibilidad.
        // El propietario puede consultar la disponibilidad
        // de los veterinarios activos de la clínica.
        if (identity.Role == UserRole.Veterinarian &&
            identity.UserId.Value != veterinarianId.Value)
        {
            throw new UnauthorizedAccessException(
                "Cannot query another veterinarian's availability.");
        }

        // 3. Verificar que el veterinario esté activo.
        await access.EnsureActiveVeterinarianAsync(
            veterinarianId,
            clinicId,
            cancellationToken);

        // 4. Obtener horarios de apertura.
        var intervals = await clinics.GetOpeningIntervalsAsync(
            query.ClinicId,
            query.Date.DayOfWeek,
            cancellationToken);

        // 5. Buscar citas del veterinario.
        var appointmentsForDay = await appointments.FindAgendaAsync(
            veterinarianId,
            query.Date,
            query.Date,
            cancellationToken);

        // Las citas canceladas no ocupan horarios.
        var occupied = appointmentsForDay
            .Where(a => a.Status != AppointmentStatus.Cancelled)
            .Select(a => a.Slot)
            .ToArray();

        // Hora local de Perú.
        var localNow = access.UtcNow.AddHours(-5);

        var durationTicks = TimeSpan
            .FromMinutes(AppointmentSlot.DurationMinutes)
            .Ticks;

        var available = new List<AvailableSlotData>();

        foreach (var interval in intervals)
        {
            // Alinear horarios a bloques de 30 minutos.
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

                // No ofrecer fechas y horas pasadas.
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
