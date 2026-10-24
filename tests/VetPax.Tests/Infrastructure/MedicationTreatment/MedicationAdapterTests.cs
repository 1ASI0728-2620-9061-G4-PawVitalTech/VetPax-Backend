
using NSubstitute;
using VetPax.Domain.Modules.PetClinicalCare.Aggregates;
using VetPax.Domain.Modules.PetClinicalCare.Repositories;
using VetPax.Domain.Modules.PetClinicalCare.ValueObjects;
using VetPax.Infrastructure.Modules.MedicationTreatment.Adapters;
using Xunit;

namespace VetPax.Tests.Infrastructure.MedicationTreatment;

public sealed class MedicationAdapterTests
{
    [Fact]
    public async Task PatientAdapter_EmptyPetId_ReturnsNull()
    {
        var repository = Substitute.For<IPetRepository>();
        var adapter = new MedicationPatientAdapter(repository);

        var result = await adapter.GetOwnerIdAsync(Guid.Empty);

        Assert.Null(result);
    }

    [Fact]
    public async Task PatientAdapter_PetNotFound_ReturnsNull()
    {
        var repository = Substitute.For<IPetRepository>();

        repository.FindByIdAsync(
                Arg.Any<PetId>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Pet?>(null));

        var adapter = new MedicationPatientAdapter(repository);

        var result = await adapter.GetOwnerIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task ClinicalContext_EmptyAttentionId_ReturnsFalse()
    {
        var repository =
            Substitute.For<IClinicalRecordRepository>();

        var adapter = new MedicationClinicalContextAdapter(
            repository);

        var result = await adapter.MatchesAsync(
            Guid.Empty,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid());

        Assert.False(result);
    }

    [Fact]
    public async Task ClinicalContext_MissingRecord_ReturnsFalse()
    {
        var repository =
            Substitute.For<IClinicalRecordRepository>();

        repository.FindByPetIdAsync(
                Arg.Any<PetId>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ClinicalRecord?>(null));

        var adapter = new MedicationClinicalContextAdapter(
            repository);

        var result = await adapter.MatchesAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid());

        Assert.False(result);
    }
}
