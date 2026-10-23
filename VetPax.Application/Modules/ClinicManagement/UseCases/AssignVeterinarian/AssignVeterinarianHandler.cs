
using VetPax.Application.Common.Ports;
using VetPax.Application.Modules.ClinicManagement.Ports;
using VetPax.Application.Modules.IdentityAccessManagement.Ports;
using VetPax.Domain.Modules.ClinicManagement.Repositories;
using VetPax.Domain.Modules.ClinicManagement.ValueObjects;
using VetPax.Domain.Modules.IdentityAccessManagement.ValueObjects;

namespace VetPax.Application.Modules.ClinicManagement.UseCases.AssignVeterinarian;

public sealed record AssignVeterinarianCommand(
    Guid ClinicId,
    Guid VeterinarianId);

public sealed class AssignVeterinarianHandler(
    IAuthenticatedUserPort currentUser,
    IClinicRepository clinics,
    IVeterinarianIdentityDirectoryPort directory,
    IClinicVeterinarianWriterPort memberships,
    IUnitOfWork unitOfWork)
{
    public async Task HandleAsync(
        AssignVeterinarianCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.ClinicId == Guid.Empty ||
            command.VeterinarianId == Guid.Empty)
            throw new ArgumentException(
                "Valid clinic and veterinarian IDs are required.");

        var identity = await currentUser.GetCurrentAsync(
            cancellationToken);

        if (identity is null ||
            identity.Role != UserRole.ClinicAdministrator)
            throw new UnauthorizedAccessException(
                "Clinic administrator required.");

        var clinicId = new ClinicId(command.ClinicId);

        var clinic = await clinics.FindByIdAsync(
            clinicId, cancellationToken)
            ?? throw new KeyNotFoundException(
                "Clinic not found.");

        if (!clinic.IsManagedBy(identity.UserId.Value))
            throw new UnauthorizedAccessException(
                "Not an administrator of this clinic.");

        var verified = await directory.IsActiveVeterinarianAsync(
            command.VeterinarianId,
            cancellationToken);

        if (!verified)
            throw new UnauthorizedAccessException(
                "Target account is not an active veterinarian.");

        await memberships.LinkAsync(
            clinicId,
            command.VeterinarianId,
            cancellationToken);

        await unitOfWork.CommitAsync(cancellationToken);
    }
}
