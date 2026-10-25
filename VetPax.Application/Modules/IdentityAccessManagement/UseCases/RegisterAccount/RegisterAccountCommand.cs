namespace VetPax.Application.Modules.IdentityAccessManagement.UseCases.RegisterAccount;

public sealed record RegisterAccountCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password
);