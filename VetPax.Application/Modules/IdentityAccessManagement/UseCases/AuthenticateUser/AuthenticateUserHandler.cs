using VetPax.Application.Modules.IdentityAccessManagement.Ports;

namespace VetPax.Application.Modules.IdentityAccessManagement.UseCases.AuthenticateUser;

public sealed class AuthenticateUserHandler
{
    private readonly IAuthenticatedUserPort _currentUser;

    public AuthenticateUserHandler(
        IAuthenticatedUserPort currentUser)
    {
        _currentUser = currentUser;
    }

    public async Task<AuthenticateUserResult?> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        var identity = await _currentUser.GetCurrentAsync(
            cancellationToken);

        if (identity is null)
            return null;

        return new AuthenticateUserResult(
            identity.UserId.Value,
            identity.Email,
            identity.Role);
    }
}