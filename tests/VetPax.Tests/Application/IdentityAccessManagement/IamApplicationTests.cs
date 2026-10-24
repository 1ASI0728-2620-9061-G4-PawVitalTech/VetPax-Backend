
using NSubstitute;
using VetPax.Application.Common.Ports;
using VetPax.Application.Modules.IdentityAccessManagement.Ports;
using VetPax.Application.Modules.IdentityAccessManagement.UseCases.AuthenticateUser;
using VetPax.Application.Modules.IdentityAccessManagement.UseCases.RegisterAccount;
using VetPax.Application.Modules.IdentityAccessManagement.UseCases.ValidatePermission;
using VetPax.Domain.Modules.IdentityAccessManagement.Services;
using VetPax.Domain.Modules.IdentityAccessManagement.ValueObjects;
using VetPax.Domain.Shared.Events;
using Xunit;

namespace VetPax.Tests.Application.IdentityAccessManagement;

public sealed class IamApplicationTests
{
    [Fact]
    public async Task RegisterOwner_ProvisionsAccount()
    {
        var provisioning =
            Substitute.For<IIdentityProvisioningPort>();

        var publisher =
            Substitute.For<IDomainEventPublisher>();

        var userId = Guid.NewGuid();

        provisioning.RegisterOwnerAsync(
                "owner@example.com",
                "ExamplePassword123!",
                Arg.Any<CancellationToken>())
            .Returns(new ProvisionedIdentity(
                userId,
                "owner@example.com"));

        var handler = new RegisterAccountHandler(
            provisioning,
            publisher);

        var result = await handler.HandleAsync(
            new RegisterAccountCommand(
                "owner@example.com",
                "ExamplePassword123!"));

        Assert.Equal(userId, result.UserId);
        Assert.Equal(UserRole.Owner, result.Role);

        await provisioning.Received(1)
            .RegisterOwnerAsync(
                "owner@example.com",
                "ExamplePassword123!",
                Arg.Any<CancellationToken>());

        await publisher.Received(1)
            .PublishAsync(
                Arg.Is<IReadOnlyCollection<IDomainEvent>>(
                    events => events.Count == 1),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterOwner_RejectsInvalidEmail()
    {
        var provisioning =
            Substitute.For<IIdentityProvisioningPort>();

        var publisher =
            Substitute.For<IDomainEventPublisher>();

        var handler = new RegisterAccountHandler(
            provisioning,
            publisher);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.HandleAsync(
                new RegisterAccountCommand(
                    "not-an-email",
                    "ExamplePassword123!")));

        await provisioning.DidNotReceive()
            .RegisterOwnerAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterOwner_RejectsEmptyPassword()
    {
        var provisioning =
            Substitute.For<IIdentityProvisioningPort>();

        var handler = new RegisterAccountHandler(
            provisioning,
            Substitute.For<IDomainEventPublisher>());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.HandleAsync(
                new RegisterAccountCommand(
                    "owner@example.com",
                    "")));

        await provisioning.DidNotReceive()
            .RegisterOwnerAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AuthenticateUser_ReturnsAuthenticatedIdentity()
    {
        var current =
            Substitute.For<IAuthenticatedUserPort>();

        var userId = Guid.NewGuid();

        current.GetCurrentAsync(
                Arg.Any<CancellationToken>())
            .Returns(new AuthenticatedIdentity(
                new UserId(userId),
                "owner@example.com",
                UserRole.Owner));

        var handler = new AuthenticateUserHandler(current);

        var result = await handler.HandleAsync();

        Assert.NotNull(result);
        Assert.Equal(userId, result.UserId);
        Assert.Equal(UserRole.Owner, result.Role);
    }

    [Fact]
    public async Task AuthenticateUser_WithoutIdentity_ReturnsNull()
    {
        var current =
            Substitute.For<IAuthenticatedUserPort>();

        current.GetCurrentAsync(
                Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult<AuthenticatedIdentity?>(null));

        var handler = new AuthenticateUserHandler(current);

        Assert.Null(await handler.HandleAsync());
    }

    [Fact]
    public async Task ValidatePermission_AllowsOwnerAction()
    {
        var current =
            Substitute.For<IAuthenticatedUserPort>();

        current.GetCurrentAsync(
                Arg.Any<CancellationToken>())
            .Returns(new AuthenticatedIdentity(
                new UserId(Guid.NewGuid()),
                "owner@example.com",
                UserRole.Owner));

        var handler = new ValidatePermissionHandler(
            current,
            new RoleAccessPolicy());

        var allowed = await handler.HandleAsync(
            AccessPermission.ManageOwnPets);

        Assert.True(allowed);
    }

    [Fact]
    public async Task ValidatePermission_DeniesOtherRoleAction()
    {
        var current =
            Substitute.For<IAuthenticatedUserPort>();

        current.GetCurrentAsync(
                Arg.Any<CancellationToken>())
            .Returns(new AuthenticatedIdentity(
                new UserId(Guid.NewGuid()),
                "owner@example.com",
                UserRole.Owner));

        var handler = new ValidatePermissionHandler(
            current,
            new RoleAccessPolicy());

        var allowed = await handler.HandleAsync(
            AccessPermission.ManageClinic);

        Assert.False(allowed);
    }

    [Fact]
    public async Task ValidatePermission_WithoutIdentity_Denies()
    {
        var current =
            Substitute.For<IAuthenticatedUserPort>();

        current.GetCurrentAsync(
                Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult<AuthenticatedIdentity?>(null));

        var handler = new ValidatePermissionHandler(
            current,
            new RoleAccessPolicy());

        var allowed = await handler.HandleAsync(
            AccessPermission.ManageOwnPets);

        Assert.False(allowed);
    }
}
