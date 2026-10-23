using VetPax.Application.Modules.IdentityAccessManagement.Ports;
using VetPax.Domain.Modules.IdentityAccessManagement.Services;
using VetPax.Domain.Modules.IdentityAccessManagement.ValueObjects;

namespace VetPax.Application.Modules.IdentityAccessManagement.UseCases.ValidatePermission;

public sealed class ValidatePermissionHandler
{
    private readonly IAuthenticatedUserPort _currentUser;
    private readonly RoleAccessPolicy _policy;

    public ValidatePermissionHandler(
        IAuthenticatedUserPort currentUser,
        RoleAccessPolicy policy)
    {
        _currentUser = currentUser;
        _policy = policy;
    }

    public async Task<bool> HandleAsync(
        AccessPermission permission,
        CancellationToken cancellationToken = default)
    {
        var identity = await _currentUser.GetCurrentAsync(
            cancellationToken);

        if (identity is null)
            return false;

        return _policy.CanAccess(
            identity.Role,
            permission);
    }
}