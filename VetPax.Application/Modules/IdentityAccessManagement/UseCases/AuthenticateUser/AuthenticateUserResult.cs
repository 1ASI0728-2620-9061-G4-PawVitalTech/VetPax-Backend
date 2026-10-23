using VetPax.Domain.Modules.IdentityAccessManagement.ValueObjects;

namespace VetPax.Application.Modules.IdentityAccessManagement.UseCases.AuthenticateUser;

public sealed record AuthenticateUserResult(
    Guid UserId,
    string Email,
    UserRole Role
);