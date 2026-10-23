
using Xunit;
using VetPax.Domain.Modules.PetClinicalCare.Entities;
using VetPax.Domain.Modules.PetClinicalCare.Events;
using VetPax.Domain.Modules.PetClinicalCare.Factories;
using VetPax.Domain.Modules.PetClinicalCare.Services;
using VetPax.Domain.Modules.PetClinicalCare.ValueObjects;

namespace VetPax.Tests.Domain.PetClinicalCare;

public sealed class PetClinicalCareDomainTests
{
    [Fact]
    public void RegisterPet_ShouldCreateDomainEvent()
    {
        var pet = PetFactory.Register(
            new OwnerId(Guid.NewGuid()),
            "Luna",
            Species.Dog,
            new DateOnly(2022, 5, 10));

        Assert.NotEqual(Guid.Empty, pet.PetId.Value);
        Assert.Contains(
            pet.PullDomainEvents(),
            e => e is PetRegisteredEvent);
    }

    [Fact]
    public void ClinicalAttention_ShouldRejectMissingReason()
    {
        Assert.Throws<ArgumentException>(() =>
            new ClinicalAttention(
                AttentionId.New(),
                new ClinicId(Guid.NewGuid()),
                new VeterinarianId(Guid.NewGuid()),
                DateTime.UtcNow,
                "",
                "Healthy",
                "Observation"
            ));
    }

    [Fact]
    public void ClinicalAccessPolicy_ShouldAllowOnlyOwner()
    {
        var ownerId = Guid.NewGuid();

        var pet = PetFactory.Register(
            new OwnerId(ownerId),
            "Luna",
            Species.Dog,
            new DateOnly(2022, 5, 10));

        var policy = new ClinicalAccessPolicy();

        var owner = new AccessRequester(
            ownerId, RequesterRole.Owner);

        var stranger = new AccessRequester(
            Guid.NewGuid(), RequesterRole.Owner);

        Assert.True(policy.CanAccess(owner, pet));
        Assert.False(policy.CanAccess(stranger, pet));
    }

    [Fact]
    public void Evolution_ShouldHaveSufficientData()
    {
        var petId = PetId.New();
        var record = ClinicalRecordFactory.OpenFor(petId);

        var clinicId = new ClinicId(Guid.NewGuid());
        var vetId = new VeterinarianId(Guid.NewGuid());

        record.RegisterAttention(new ClinicalAttention(
            AttentionId.New(),
            clinicId,
            vetId,
            DateTime.UtcNow.AddDays(-7),
            "Control",
            "Stable",
            "Monitoring",
            indicators: new[]
            {
                new ClinicalIndicator("Weight", 10m, "kg")
            }
        ));

        record.RegisterAttention(new ClinicalAttention(
            AttentionId.New(),
            clinicId,
            vetId,
            DateTime.UtcNow,
            "Follow-up",
            "Stable",
            "Monitoring",
            indicators: new[]
            {
                new ClinicalIndicator("Weight", 10.5m, "kg")
            }
        ));

        var service = new ClinicalEvolutionService();
        var evolution = service.BuildEvolution(record);

        Assert.True(evolution.HasSufficientData());
        Assert.Equal(2, evolution.Entries.Count);
    }
}
