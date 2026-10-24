
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using VetPax.Domain.Modules.IdentityAccessManagement.ValueObjects;
using VetPax.Infrastructure.Modules.IdentityAccessManagement.Adapters;
using Xunit;

namespace VetPax.Tests.Infrastructure.IdentityAccessManagement;

public sealed class KeycloakAuthenticatedUserTests
{
    [Fact]
    public async Task ValidOwnerClaims_ReturnOwnerIdentity()
    {
        var userId = Guid.NewGuid();

        var adapter = CreateAdapter(
            userId.ToString(),
            "owner@example.com",
            "owner");

        var result = await adapter.GetCurrentAsync();

        Assert.NotNull(result);
        Assert.Equal(userId, result.UserId.Value);
        Assert.Equal(UserRole.Owner, result.Role);
    }

    [Fact]
    public async Task VeterinarianRole_IsMapped()
    {
        var adapter = CreateAdapter(
            Guid.NewGuid().ToString(),
            "vet@example.com",
            "veterinarian");

        var result = await adapter.GetCurrentAsync();

        Assert.NotNull(result);
        Assert.Equal(UserRole.Veterinarian, result.Role);
    }

    [Fact]
    public async Task AdministratorRole_IsMapped()
    {
        var adapter = CreateAdapter(
            Guid.NewGuid().ToString(),
            "admin@example.com",
            "clinic_administrator");

        var result = await adapter.GetCurrentAsync();

        Assert.NotNull(result);
        Assert.Equal(
            UserRole.ClinicAdministrator,
            result.Role);
    }

    [Fact]
    public async Task InvalidSubject_IsRejected()
    {
        var adapter = CreateAdapter(
            "invalid-guid",
            "owner@example.com",
            "owner");

        Assert.Null(await adapter.GetCurrentAsync());
    }

    [Fact]
    public async Task MissingEmail_IsRejected()
    {
        var adapter = CreateAdapter(
            Guid.NewGuid().ToString(),
            null,
            "owner");

        Assert.Null(await adapter.GetCurrentAsync());
    }

    [Fact]
    public async Task MultipleVetPaxRoles_AreRejected()
    {
        var adapter = CreateAdapter(
            Guid.NewGuid().ToString(),
            "owner@example.com",
            "owner",
            "veterinarian");

        Assert.Null(await adapter.GetCurrentAsync());
    }

    [Fact]
    public async Task UnauthenticatedUser_IsRejected()
    {
        var adapter = CreateAdapter(
            Guid.NewGuid().ToString(),
            "owner@example.com",
            "owner",
            authenticated: false);

        Assert.Null(await adapter.GetCurrentAsync());
    }

    private static KeycloakAuthenticatedUserAdapter CreateAdapter(
        string? subject,
        string? email,
        string role,
        string? secondRole = null,
        bool authenticated = true)
    {
        var claims = new List<Claim>();

        if (subject is not null)
            claims.Add(new Claim("sub", subject));

        if (email is not null)
            claims.Add(new Claim("email", email));

        claims.Add(new Claim(ClaimTypes.Role, role));

        if (secondRole is not null)
            claims.Add(new Claim(
                ClaimTypes.Role,
                secondRole));

        var identity = new ClaimsIdentity(
            claims,
            authenticated ? "Bearer" : null);

        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity)
        };

        var accessor = new HttpContextAccessor
        {
            HttpContext = context
        };

        return new KeycloakAuthenticatedUserAdapter(accessor);
    }
}
