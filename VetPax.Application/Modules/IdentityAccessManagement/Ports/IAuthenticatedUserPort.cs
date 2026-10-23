using VetPax.Domain.Modules.IdentityAccessManagement.ValueObjects;

namespace VetPax.Application.Modules.IdentityAccessManagement.Ports;

public sealed record AuthenticatedIdentity(
    UserId UserId,
    string Email,
    UserRole Role
);

public interface IAuthenticatedUserPort
{
    Task<AuthenticatedIdentity?> GetCurrentAsync(
        CancellationToken cancellationToken = default);
}