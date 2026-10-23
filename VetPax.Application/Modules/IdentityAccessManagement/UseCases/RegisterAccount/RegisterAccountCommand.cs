namespace VetPax.Application.Modules.IdentityAccessManagement.UseCases.RegisterAccount;

public sealed record RegisterAccountCommand(
    string Email,
    string Password
);