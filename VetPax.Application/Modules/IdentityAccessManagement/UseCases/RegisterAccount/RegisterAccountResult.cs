using VetPax.Domain.Modules.IdentityAccessManagement.ValueObjects;

namespace VetPax.Application.Modules.IdentityAccessManagement.UseCases.RegisterAccount;

public sealed record RegisterAccountResult(
    Guid UserId,
    string Email,
    UserRole Role
);