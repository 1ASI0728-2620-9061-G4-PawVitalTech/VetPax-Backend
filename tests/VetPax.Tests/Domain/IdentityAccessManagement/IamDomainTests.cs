
using VetPax.Domain.Modules.IdentityAccessManagement.Aggregates;
using VetPax.Domain.Modules.IdentityAccessManagement.Events;
using VetPax.Domain.Modules.IdentityAccessManagement.Services;
using VetPax.Domain.Modules.IdentityAccessManagement.ValueObjects;
using Xunit;

namespace VetPax.Tests.Domain.IdentityAccessManagement;

public sealed class IamDomainTests
{
    private readonly RoleAccessPolicy _policy = new();

    [Fact]
    public void CreateAccount_AssignsSpecifiedRole()
    {
        var userId = Guid.NewGuid();

        var account = UserAccount.CreateFromVerifiedIdentity(
            new UserId(userId),
            "owner@example.com",
            UserRole.Owner);

        Assert.Equal(userId, account.UserId.Value);
        Assert.Equal(UserRole.Owner, account.Role);
        Assert.Equal("owner@example.com", account.Email);
    }

    [Fact]
    public void CreateAccount_EmitsCreatedEvent()
    {
        var account = UserAccount.CreateFromVerifiedIdentity(
            new UserId(Guid.NewGuid()),
            "owner@example.com",
            UserRole.Owner);

        var domainEvent = Assert.Single(
            account.PullDomainEvents());

        Assert.IsType<UserAccountCreatedEvent>(domainEvent);
    }

    [Fact]
    public void PullEvents_RemovesPendingEvents()
    {
        var account = UserAccount.CreateFromVerifiedIdentity(
            new UserId(Guid.NewGuid()),
            "owner@example.com",
            UserRole.Owner);

        Assert.Single(account.PullDomainEvents());
        Assert.Empty(account.PullDomainEvents());
    }

    [Fact]
    public void EmptyUserId_IsRejected()
    {
        Assert.Throws<ArgumentException>(() =>
            UserAccount.CreateFromVerifiedIdentity(
                new UserId(Guid.Empty),
                "owner@example.com",
                UserRole.Owner));
    }

    [Theory]
    [InlineData(
        UserRole.Owner,
        AccessPermission.ManageOwnPets,
        true)]
    [InlineData(
        UserRole.Owner,
        AccessPermission.ManageClinic,
        false)]
    [InlineData(
        UserRole.Veterinarian,
        AccessPermission.RegisterClinicalAttention,
        true)]
    [InlineData(
        UserRole.Veterinarian,
        AccessPermission.ManageOwnPets,
        false)]
    [InlineData(
        UserRole.ClinicAdministrator,
        AccessPermission.ManageClinic,
        true)]
    [InlineData(
        UserRole.ClinicAdministrator,
        AccessPermission.RegisterClinicalAttention,
        false)]
    public void Policy_EnforcesRolePermissions(
        UserRole role,
        AccessPermission permission,
        bool expected)
    {
        Assert.Equal(
            expected,
            _policy.CanAccess(role, permission));
    }
}
