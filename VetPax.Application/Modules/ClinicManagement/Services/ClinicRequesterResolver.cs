using VetPax.Application.Modules.ClinicManagement.Ports;
using VetPax.Application.Modules.IdentityAccessManagement.Ports;
using VetPax.Domain.Modules.ClinicManagement.Aggregates;
using VetPax.Domain.Modules.ClinicManagement.ValueObjects;
using VetPax.Domain.Modules.IdentityAccessManagement.ValueObjects;

namespace VetPax.Application.Modules.ClinicManagement.Services;

public sealed class ClinicRequesterResolver
{
    private readonly IAuthenticatedUserPort _currentUser;
    private readonly IClinicMembershipPort _membership;

    public ClinicRequesterResolver(
        IAuthenticatedUserPort currentUser,
        IClinicMembershipPort membership)
    {
        _currentUser = currentUser;
        _membership = membership;
    }

    public async Task<ClinicAccessRequester> ResolveAsync(
        Clinic clinic,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(clinic);

        var identity = await _currentUser.GetCurrentAsync(
            cancellationToken);

        if (identity is null)
            throw new UnauthorizedAccessException(
                "Authentication required.");

        var userId = identity.UserId.Value;

        if (identity.Role == UserRole.ClinicAdministrator &&
            clinic.IsManagedBy(userId))
        {
            return new ClinicAccessRequester(
                userId,
                ClinicRequesterRole.ClinicAdministrator,
                clinic.ClinicId);
        }

        if (identity.Role == UserRole.Veterinarian &&
            await _membership.IsActiveVeterinarianAsync(
                userId,
                clinic.ClinicId,
                cancellationToken))
        {
            return new ClinicAccessRequester(
                userId,
                ClinicRequesterRole.Veterinarian,
                clinic.ClinicId);
        }

        throw new UnauthorizedAccessException(
            "User is not authorized for this clinic.");
    }
}