namespace VetPax.Application.Modules.IdentityAccessManagement.Ports;

public sealed record ProvisionedIdentity(
    Guid UserId,
    string Email
);

public interface IIdentityProvisioningPort
{
    Task<ProvisionedIdentity> RegisterOwnerAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);
}