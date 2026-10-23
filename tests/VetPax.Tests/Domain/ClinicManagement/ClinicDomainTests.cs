using VetPax.Domain.Modules.ClinicManagement.Aggregates;
using VetPax.Domain.Modules.ClinicManagement.Services;
using VetPax.Domain.Modules.ClinicManagement.ValueObjects;
using Xunit;

namespace VetPax.Tests.Domain.ClinicManagement;

public sealed class ClinicDomainTests
{
    [Fact]
    public void OpeningSchedule_RejectsOverlappingIntervals()
    {
        Assert.Throws<ArgumentException>(() =>
            new OpeningSchedule(new[]
            {
                new DailyOpeningHours(
                    DayOfWeek.Monday,
                    new TimeOnly(9, 0),
                    new TimeOnly(13, 0)),

                new DailyOpeningHours(
                    DayOfWeek.Monday,
                    new TimeOnly(12, 0),
                    new TimeOnly(18, 0))
            }));
    }

    [Fact]
    public void ClinicAccessPolicy_DeniesDifferentAdministrator()
    {
        var adminId = Guid.NewGuid();
        var clinicId = ClinicId.New();

        var clinic = new Clinic(
            clinicId,
            new AdministratorId(adminId),
            new ClinicGeneralData(
                "VetPax",
                "Av. Central 123"),
            new OpeningSchedule(
                Array.Empty<DailyOpeningHours>()));

        var policy = new ClinicAccessPolicy();

        var stranger = new ClinicAccessRequester(
            Guid.NewGuid(),
            ClinicRequesterRole.ClinicAdministrator,
            clinicId);

        Assert.False(
            policy.CanManageProfile(stranger, clinic));

        var administrator = new ClinicAccessRequester(
            adminId,
            ClinicRequesterRole.ClinicAdministrator,
            clinicId);

        Assert.True(
            policy.CanManageProfile(administrator, clinic));
    }
}