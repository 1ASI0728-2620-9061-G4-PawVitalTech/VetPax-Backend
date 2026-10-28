using VetPax.Application.Modules.IdentityAccessManagement
    .UseCases.AuthenticateUser;
using VetPax.Application.Modules.IdentityAccessManagement
    .UseCases.RegisterAccount;
using VetPax.Domain.Modules.IdentityAccessManagement.ValueObjects;

namespace VetPax.Api.Modules.IdentityAccessManagement.Resources;

public sealed record RegisterAccountResponse(
    Guid UserId,
    string Email,
    string Role)
{
    public static RegisterAccountResponse From(
        RegisterAccountResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new RegisterAccountResponse(
            result.UserId,
            result.Email,
            IdentityRoleNames.From(result.Role));
    }
}

public sealed record AuthenticatedUserResponse(
    Guid UserId,
    string Email,
    string Role)
{
    public static AuthenticatedUserResponse From(
        AuthenticateUserResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new AuthenticatedUserResponse(
            result.UserId,
            result.Email,
            IdentityRoleNames.From(result.Role));
    }
}

public static class IdentityRoleNames
{
    public static string From(UserRole role)
    {
        return role switch
        {
            UserRole.Owner => "owner",

            UserRole.Veterinarian =>
                "veterinarian",

            UserRole.ClinicAdministrator =>
                "clinic_administrator",

            _ => throw new ArgumentOutOfRangeException(
                nameof(role),
                role,
                "Unsupported IAM role.")
        };
    }
}