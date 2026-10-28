
using VetPax.Application.Modules.AppointmentManagement.Ports;
using VetPax.Application.Modules.IdentityAccessManagement.Ports;
using VetPax.Domain.Modules.AppointmentManagement.Aggregates;
using VetPax.Domain.Modules.AppointmentManagement.ValueObjects;
using VetPax.Domain.Modules.IdentityAccessManagement.ValueObjects;

namespace VetPax.Application.Modules.AppointmentManagement.Services;

public sealed class AppointmentAccessService
{
    private static readonly TimeSpan PeruUtcOffset =
        TimeSpan.FromHours(-5);

    private readonly IAuthenticatedUserPort _currentUser;
    private readonly IAppointmentPatientPort _patients;
    private readonly IAppointmentClinicPort _clinics;
    private readonly TimeProvider _clock;

    public AppointmentAccessService(
        IAuthenticatedUserPort currentUser,
        IAppointmentPatientPort patients,
        IAppointmentClinicPort clinics,
        TimeProvider clock)
    {
        _currentUser = currentUser;
        _patients = patients;
        _clinics = clinics;
        _clock = clock;
    }

    public DateTime UtcNow =>
        _clock.GetUtcNow().UtcDateTime;

    public async Task<AppointmentOwnerId> RequireOwnerAsync(
        CancellationToken cancellationToken = default)
    {
        var identity = await _currentUser.GetCurrentAsync(
            cancellationToken);

        if (identity is null ||
            identity.Role != UserRole.Owner ||
            identity.UserId.Value == Guid.Empty)
        {
            throw new UnauthorizedAccessException(
                "Authenticated pet owner required.");
        }

        return new AppointmentOwnerId(identity.UserId.Value);
    }

    public async Task<AppointmentVeterinarianId>
        RequireVeterinarianAsync(
            CancellationToken cancellationToken = default)
    {
        var identity = await _currentUser.GetCurrentAsync(
            cancellationToken);

        if (identity is null ||
            identity.Role != UserRole.Veterinarian ||
            identity.UserId.Value == Guid.Empty)
        {
            throw new UnauthorizedAccessException(
                "Authenticated veterinarian required.");
        }

        return new AppointmentVeterinarianId(
            identity.UserId.Value);
    }

    public async Task EnsureCanBookAsync(
        AppointmentOwnerId ownerId,
        AppointmentPetId petId,
        AppointmentClinicId clinicId,
        AppointmentVeterinarianId veterinarianId,
        AppointmentSlot slot,
        CancellationToken cancellationToken = default)
    {
        EnsureFutureSlot(slot);

        var ownsLinkedPet = await _patients
            .IsOwnedAndLinkedAsync(
                petId.Value,
                ownerId.Value,
                clinicId.Value,
                cancellationToken);

        if (!ownsLinkedPet)
        {
            throw new UnauthorizedAccessException(
                "Pet is not owned by this user or linked to this clinic.");
        }

        await EnsureActiveVeterinarianAsync(
            veterinarianId,
            clinicId,
            cancellationToken);

        var withinHours = await _clinics
            .IsWithinOpeningHoursAsync(
                clinicId.Value,
                slot,
                cancellationToken);

        if (!withinHours)
        {
            throw new ArgumentException(
                "Appointment is outside clinic opening hours.");
        }
    }

    public async Task EnsureActiveVeterinarianAsync(
        AppointmentVeterinarianId veterinarianId,
        AppointmentClinicId clinicId,
        CancellationToken cancellationToken = default)
    {
        var active = await _clinics.IsActiveVeterinarianAsync(
            veterinarianId.Value,
            clinicId.Value,
            cancellationToken);

        if (!active)
        {
            throw new UnauthorizedAccessException(
                "Veterinarian is not assigned to this clinic.");
        }
    }

    public static void EnsureOwnedBy(
        Appointment appointment,
        AppointmentOwnerId ownerId)
    {
        if (!appointment.IsOwnedBy(ownerId))
        {
            throw new UnauthorizedAccessException(
                "Appointment does not belong to this owner.");
        }
    }

    public void EnsureFutureSlot(AppointmentSlot slot)
    {
        ArgumentNullException.ThrowIfNull(slot);

        var localNow = _clock.GetUtcNow()
            .ToOffset(PeruUtcOffset);

        var appointmentDateTime = slot.Date
            .ToDateTime(slot.StartTime);

        if (appointmentDateTime <= localNow.DateTime)
        {
            throw new ArgumentException(
                "Appointment must be scheduled in the future.");
        }
    }
}
