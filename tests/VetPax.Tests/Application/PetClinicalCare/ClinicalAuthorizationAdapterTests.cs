
using NSubstitute;
using VetPax.Application.Modules.PetClinicalCare.Ports;
using VetPax.Domain.Modules.PetClinicalCare.Services;
using VetPax.Domain.Modules.PetClinicalCare.ValueObjects;
using VetPax.Infrastructure.Modules.PetClinicalCare.Adapters;
using Xunit;

namespace VetPax.Tests.Application.PetClinicalCare;

public sealed class ClinicalAuthorizationAdapterTests
{
    [Theory]
    [InlineData(true, true, true)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    public async Task Authorization_RequiresBothActiveLinks(
        bool clinicMembership,
        bool patientLink,
        bool expected)
    {
        var veterinarianId = Guid.NewGuid();
        var clinicId = Guid.NewGuid();
        var petId = PetId.New();

        var relationships =
            Substitute.For<IClinicalRelationshipPort>();

        relationships.HasActiveVeterinarianMembershipAsync(
            veterinarianId,
            clinicId,
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(clinicMembership));

        relationships.HasActivePatientLinkAsync(
            petId.Value,
            clinicId,
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(patientLink));

        var adapter = new VerifiedClinicalAuthorizationAdapter(
            relationships,
            new VeterinaryClinicalAccessPolicy());

        var requester = new AccessRequester(
            veterinarianId,
            RequesterRole.Veterinarian,
            new ClinicId(clinicId));

        var result = await adapter.IsAuthorizedAsync(
            petId,
            requester);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task Authorization_DeniesOwnerRole()
    {
        var relationships =
            Substitute.For<IClinicalRelationshipPort>();

        var adapter = new VerifiedClinicalAuthorizationAdapter(
            relationships,
            new VeterinaryClinicalAccessPolicy());

        var requester = new AccessRequester(
            Guid.NewGuid(),
            RequesterRole.Owner,
            new ClinicId(Guid.NewGuid()));

        var result = await adapter.IsAuthorizedAsync(
            PetId.New(),
            requester);

        Assert.False(result);

        await relationships.DidNotReceive()
            .HasActiveVeterinarianMembershipAsync(
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>());
    }
}
