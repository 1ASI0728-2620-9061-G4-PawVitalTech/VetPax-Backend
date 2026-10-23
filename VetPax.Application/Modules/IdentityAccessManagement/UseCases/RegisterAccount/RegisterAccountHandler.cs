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

        if (string.IsNullOrWhiteSpace(command.Email) ||
            !MailAddress.TryCreate(command.Email, out _))
            throw new ArgumentException("Invalid email.");

        if (string.IsNullOrWhiteSpace(command.Password))
            throw new ArgumentException("Password is required.");

        var identity = await _identityProvider.RegisterOwnerAsync(
            command.Email.Trim(),
            command.Password,
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
}