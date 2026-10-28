
using VetPax.Application.Modules.IdentityAccessManagement.Ports;
using VetPax.Application.Modules.MedicationTreatment.Ports;
using VetPax.Domain.Modules.IdentityAccessManagement.ValueObjects;
using VetPax.Domain.Modules.MedicationTreatment.Aggregates;

namespace VetPax.Application.Modules.MedicationTreatment.Services;

public sealed class MedicationAuthorizationService
{
    private readonly IAuthenticatedUserPort _currentUser;
    private readonly IMedicationPatientPort _patients;
    private readonly IMedicationVeterinarianPort _veterinarians;

    public MedicationAuthorizationService(
        IAuthenticatedUserPort currentUser,
        IMedicationPatientPort patients,
        IMedicationVeterinarianPort veterinarians)
    {
        _currentUser = currentUser;
        _patients = patients;
        _veterinarians = veterinarians;
    }

    public async Task<(Guid VeterinarianId, Guid OwnerId)>
        RequirePrescriberAsync(
            Guid petId,
            Guid clinicId,
            CancellationToken cancellationToken = default)
    {
        if (petId == Guid.Empty || clinicId == Guid.Empty)
            throw new ArgumentException(
                "Valid pet and clinic IDs are required.");

        var identity = await _currentUser.GetCurrentAsync(
            cancellationToken);

        if (identity is null ||
            identity.UserId.Value == Guid.Empty)
            throw new UnauthorizedAccessException(
                "Authentication required.");

        if (identity.Role != UserRole.Veterinarian)
            throw new UnauthorizedAccessException(
                "Veterinarian role required.");

        var ownerId = await _patients.GetOwnerIdAsync(
            petId,
            cancellationToken);

        if (ownerId is null)
            throw new KeyNotFoundException("Pet not found.");

        var authorized = await _veterinarians.CanPrescribeAsync(
            identity.UserId.Value,
            clinicId,
            petId,
            cancellationToken);

        if (!authorized)
            throw new UnauthorizedAccessException(
                "Veterinarian is not authorized for this patient.");

        return (identity.UserId.Value, ownerId.Value);
    }

    public async Task<Guid> RequireOwnerAsync(
        Guid petId,
        CancellationToken cancellationToken = default)
    {
        var identity = await _currentUser.GetCurrentAsync(
            cancellationToken);

        if (identity is null ||
            identity.UserId.Value == Guid.Empty)
            throw new UnauthorizedAccessException(
                "Authentication required.");

        if (identity.Role != UserRole.Owner)
            throw new UnauthorizedAccessException(
                "Owner role required.");

        var ownerId = await _patients.GetOwnerIdAsync(
            petId,
            cancellationToken);

        if (ownerId is null)
            throw new KeyNotFoundException("Pet not found.");

        if (ownerId.Value != identity.UserId.Value)
            throw new UnauthorizedAccessException(
                "Pet belongs to another owner.");

        return identity.UserId.Value;
    }

    public async Task<Guid> RequirePlanPrescriberAsync(
        MedicationPlan plan,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var (veterinarianId, ownerId) =
            await RequirePrescriberAsync(
                plan.PetId.Value,
                plan.ClinicId.Value,
                cancellationToken);

        if (plan.VeterinarianId.Value != veterinarianId ||
            plan.OwnerId.Value != ownerId)
        {
            throw new UnauthorizedAccessException(
                "Veterinarian cannot modify this prescription.");
        }

        return veterinarianId;
    }

    public async Task<Guid> RequirePlanOwnerAsync(
        MedicationPlan plan,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var ownerId = await RequireOwnerAsync(
            plan.PetId.Value,
            cancellationToken);

        if (plan.OwnerId.Value != ownerId)
            throw new UnauthorizedAccessException(
                "Treatment belongs to another owner.");

        return ownerId;
    }

    public async Task EnsureCanReadAsync(
        MedicationPlan plan,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var identity = await _currentUser.GetCurrentAsync(
            cancellationToken);

        if (identity is null ||
            identity.UserId.Value == Guid.Empty)
            throw new UnauthorizedAccessException(
                "Authentication required.");

        if (identity.Role == UserRole.Owner)
        {
            await RequirePlanOwnerAsync(
                plan,
                cancellationToken);

            return;
        }

        if (identity.Role == UserRole.Veterinarian)
        {
            await RequirePlanPrescriberAsync(
                plan,
                cancellationToken);

            return;
        }

        throw new UnauthorizedAccessException(
            "Insufficient permissions to read medication plans.");
    }
}
