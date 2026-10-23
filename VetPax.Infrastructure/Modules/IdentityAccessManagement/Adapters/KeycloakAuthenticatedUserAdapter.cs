using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using VetPax.Application.Modules.IdentityAccessManagement.Ports;
using VetPax.Domain.Modules.IdentityAccessManagement.ValueObjects;

namespace VetPax.Infrastructure.Modules.IdentityAccessManagement.Adapters;

public sealed class KeycloakAuthenticatedUserAdapter(
    IHttpContextAccessor contextAccessor
) : IAuthenticatedUserPort
{
    public Task<AuthenticatedIdentity?> GetCurrentAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = contextAccessor.HttpContext?.User;

        if (user?.Identity?.IsAuthenticated != true)
            return Task.FromResult<AuthenticatedIdentity?>(null);

        if (!Guid.TryParse(
                user.FindFirst("sub")?.Value, out var userId))
            return Task.FromResult<AuthenticatedIdentity?>(null);

        var email = user.FindFirst("email")?.Value;

        if (string.IsNullOrWhiteSpace(email))
            return Task.FromResult<AuthenticatedIdentity?>(null);

        var roles = new[]
            {
                (Name: "owner", Role: UserRole.Owner),
                (Name: "veterinarian", Role: UserRole.Veterinarian),
                (Name: "clinic_administrator",
                    Role: UserRole.ClinicAdministrator)
            }
            .Where(r => user.IsInRole(r.Name))
            .ToArray();

        if (roles.Length != 1)
            return Task.FromResult<AuthenticatedIdentity?>(null);

        AuthenticatedIdentity identity = new(
            new UserId(userId),
            email,
            roles[0].Role
        );

        return Task.FromResult<AuthenticatedIdentity?>(identity);
    }
}