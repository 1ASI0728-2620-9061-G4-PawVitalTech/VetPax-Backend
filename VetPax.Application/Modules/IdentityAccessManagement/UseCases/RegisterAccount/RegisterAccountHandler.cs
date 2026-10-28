using System.Net.Mail;
using VetPax.Application.Common.Ports;
using VetPax.Application.Modules.IdentityAccessManagement.Ports;
using VetPax.Domain.Modules.IdentityAccessManagement.Aggregates;
using VetPax.Domain.Modules.IdentityAccessManagement.ValueObjects;

namespace VetPax.Application.Modules.IdentityAccessManagement.UseCases.RegisterAccount;

public sealed class RegisterAccountHandler
{
    private readonly IIdentityProvisioningPort _identityProvider;
    private readonly IDomainEventPublisher _events;

    public RegisterAccountHandler(
        IIdentityProvisioningPort identityProvider,
        IDomainEventPublisher events)
    {
        _identityProvider = identityProvider;
        _events = events;
    }

    public async Task<RegisterAccountResult> HandleAsync(
        RegisterAccountCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var firstName = NormalizeName(command.FirstName, "First name");
        var lastName = NormalizeName(command.LastName, "Last name");

        if (string.IsNullOrWhiteSpace(command.Email) ||
            !MailAddress.TryCreate(command.Email, out _))
            throw new ArgumentException("Invalid email.");

        if (string.IsNullOrWhiteSpace(command.Password))
            throw new ArgumentException("Password is required.");

        var email = command.Email.Trim();
        var password = command.Password.Trim();

        if (password.Length < 8 || password.Length > 128)
            throw new ArgumentException("Password must be between 8 and 128 characters.");

        var identity = await _identityProvider.RegisterOwnerAsync(
            firstName,
            lastName,
            email,
            password,
            cancellationToken);

        var account = UserAccount.CreateFromVerifiedIdentity(
            new UserId(identity.UserId),
            identity.Email,
            UserRole.Owner);

        await _events.PublishAsync(
            account.PullDomainEvents(),
            cancellationToken);

        return new RegisterAccountResult(
            account.UserId.Value,
            account.Email,
            account.Role);
    }

    private static string NormalizeName(string raw, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new ArgumentException($"{fieldName} is required.");

        var trimmed = raw.Trim();
        if (trimmed.Length < 2 || trimmed.Length > 80)
            throw new ArgumentException($"{fieldName} must be between 2 and 80 characters.");

        if (trimmed.Any(char.IsControl))
            throw new ArgumentException($"{fieldName} contains invalid characters.");

        return trimmed;
    }
}