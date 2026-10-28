
using NSubstitute;
using VetPax.Application.Modules.IdentityAccessManagement.Ports;
using VetPax.Application.Modules.PetClinicalCare.Ports;
using VetPax.Application.Modules.PetClinicalCare.UseCases.AuthorizeVeterinaryAccess;
using VetPax.Domain.Modules.IdentityAccessManagement.ValueObjects;
using VetPax.Domain.Modules.PetClinicalCare.Services;
using VetPax.Domain.Modules.PetClinicalCare.ValueObjects;
using Xunit;

namespace VetPax.Tests.Application.PetClinicalCare;

public sealed class VeterinaryClinicalAuthorizationTests
{
    [Theory]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, true)]
    public void Policy_RequiresBothActiveRelationships(
        bool clinicMembership,
        bool patientLink,
        bool expected)
    {
        var requester = new AccessRequester(
            Guid.NewGuid(),
            RequesterRole.Veterinarian,
            new ClinicId(Guid.NewGuid()));

        var policy = new VeterinaryClinicalAccessPolicy();

        var result = policy.CanAccess(
            requester,
            clinicMembership,
            patientLink);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task Handler_AuthorizesVerifiedVeterinarian()
    {
        var veterinarianId = Guid.NewGuid();
        var clinicId = Guid.NewGuid();
        var petId = Guid.NewGuid();

        var currentUser = Substitute.For<IAuthenticatedUserPort>();

        currentUser.GetCurrentAsync(
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AuthenticatedIdentity?>(
                new AuthenticatedIdentity(
                    new UserId(veterinarianId),
                    "veterinarian@test.local",
                    UserRole.Veterinarian)));

        var relationships =
            Substitute.For<IClinicalRelationshipPort>();

        relationships.HasActiveVeterinarianMembershipAsync(
            veterinarianId,
            clinicId,
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        relationships.HasActivePatientLinkAsync(
            petId,
            clinicId,
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        var handler = new AuthorizeVeterinaryAccessHandler(
            currentUser,
            relationships,
            new VeterinaryClinicalAccessPolicy());

        var requester = await handler.HandleAsync(
            new AuthorizeVeterinaryAccessQuery(
                petId,
                clinicId));

        Assert.Equal(veterinarianId, requester.UserId);
        Assert.Equal(RequesterRole.Veterinarian, requester.Role);
        Assert.Equal(clinicId, requester.ClinicId!.Value.Value);
    }

    [Fact]
    public async Task Handler_DeniesOwnerEvenWithActiveRelationships()
    {
        var userId = Guid.NewGuid();
        var clinicId = Guid.NewGuid();
        var petId = Guid.NewGuid();

        var currentUser = Substitute.For<IAuthenticatedUserPort>();

        currentUser.GetCurrentAsync(
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AuthenticatedIdentity?>(
                new AuthenticatedIdentity(
                    new UserId(userId),
                    "owner@test.local",
                    UserRole.Owner)));

        var relationships =
            Substitute.For<IClinicalRelationshipPort>();

        var handler = new AuthorizeVeterinaryAccessHandler(
            currentUser,
            relationships,
            new VeterinaryClinicalAccessPolicy());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.HandleAsync(
                new AuthorizeVeterinaryAccessQuery(
                    petId,
                    clinicId)));

        await relationships.DidNotReceive()
            .HasActiveVeterinarianMembershipAsync(
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handler_DeniesUnlinkedPatient()
    {
        var veterinarianId = Guid.NewGuid();
        var clinicId = Guid.NewGuid();
        var petId = Guid.NewGuid();

        var currentUser = Substitute.For<IAuthenticatedUserPort>();

        currentUser.GetCurrentAsync(
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AuthenticatedIdentity?>(
                new AuthenticatedIdentity(
                    new UserId(veterinarianId),
                    "veterinarian@test.local",
                    UserRole.Veterinarian)));

        var relationships =
            Substitute.For<IClinicalRelationshipPort>();

        relationships.HasActiveVeterinarianMembershipAsync(
            veterinarianId,
            clinicId,
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        relationships.HasActivePatientLinkAsync(
            petId,
            clinicId,
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));

        var handler = new AuthorizeVeterinaryAccessHandler(
            currentUser,
            relationships,
            new VeterinaryClinicalAccessPolicy());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.HandleAsync(
                new AuthorizeVeterinaryAccessQuery(
                    petId,
                    clinicId)));
    }
}
