
using System.Text.Json;
using VetPax.Api.Modules.MedicationTreatment.Resources;
using VetPax.Application.Modules.MedicationTreatment.Exceptions;
using VetPax.Application.Modules.MedicationTreatment.Models;
using VetPax.Domain.Modules.MedicationTreatment.Enums;
using Xunit;

namespace VetPax.Tests.Api.MedicationTreatment;

public sealed class MedicationApiContractsTests
{
    [Fact]
    public void ScheduleRequest_ConvertsWeekdays()
    {
        var schedules = new[]
        {
            new MedicationScheduleRequest(
                new TimeOnly(8, 0),
                new[] { "Monday", "WEDNESDAY" },
                1m,
                "tablet")
        };

        var result = MedicationRequestMapper.ToInputs(schedules);

        Assert.Single(result);
        Assert.Equal(
            new[] { DayOfWeek.Monday, DayOfWeek.Wednesday },
            result[0].DaysOfWeek);
    }

    [Fact]
    public void ScheduleRequest_RejectsInvalidWeekday()
    {
        var schedules = new[]
        {
            new MedicationScheduleRequest(
                new TimeOnly(8, 0),
                new[] { "NotAWeekday" },
                1m,
                "tablet")
        };

        Assert.Throws<MedicationValidationException>(() =>
            MedicationRequestMapper.ToInputs(schedules));
    }

    [Fact]
    public void ScheduleRequest_RejectsNumericWeekday()
    {
        var schedules = new[]
        {
            new MedicationScheduleRequest(
                new TimeOnly(8, 0),
                new[] { "1" },
                1m,
                "tablet")
        };

        Assert.Throws<MedicationValidationException>(() =>
            MedicationRequestMapper.ToInputs(schedules));
    }

    [Theory]
    [InlineData(
        "changeSchedules",
        MedicationUpdateOperation.ChangeSchedules)]
    [InlineData(
        "complete",
        MedicationUpdateOperation.Complete)]
    [InlineData(
        "cancel",
        MedicationUpdateOperation.Cancel)]
    public void UpdateOperation_ParsesValidOperations(
        string value,
        MedicationUpdateOperation expected)
    {
        Assert.Equal(
            expected,
            MedicationRequestMapper.ParseOperation(value));
    }

    [Fact]
    public void UpdateOperation_RejectsUnknownOperation()
    {
        Assert.Throws<MedicationValidationException>(() =>
            MedicationRequestMapper.ParseOperation("delete"));
    }

    [Fact]
    public void PlanResponse_MapsDomainEnumsToStrings()
    {
        var plan = new MedicationPlanData(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            "Test medication",
            "Oral",
            "Test instructions",
            new DateOnly(2026, 11, 1),
            new DateOnly(2026, 11, 8),
            "America/Lima",
            MedicationPlanStatus.Active,
            true,
            1,
            new[]
            {
                new MedicationScheduleData(
                    Guid.NewGuid(),
                    new TimeOnly(8, 0),
                    new[]
                    {
                        DayOfWeek.Monday,
                        DayOfWeek.Friday
                    },
                    1m,
                    "tablet")
            });

        var response = MedicationPlanResponse.From(plan);

        Assert.Equal("Active", response.Status);
        Assert.True(response.RemindersEnabled);
        Assert.Single(response.Schedules);
        Assert.Equal(
            new[] { "Monday", "Friday" },
            response.Schedules[0].DaysOfWeek);
    }

    [Fact]
    public void ReminderRequest_RequiresEnabledProperty()
    {
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize
                <ConfigureMedicationRemindersRequest>("{}"));
    }

    [Fact]
    public void AdministrationRequest_RequiresTimestamp()
    {
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize
                <RegisterMedicationAdministrationRequest>("{}"));
    }

    [Fact]
    public void AdministrationRequest_NormalizesOffsetToUtc()
    {
        var request =
            new RegisterMedicationAdministrationRequest(
                new DateTimeOffset(
                    2026, 11, 2, 8, 0, 0,
                    TimeSpan.FromHours(-5)));

        Assert.Equal(
            new DateTime(
                2026, 11, 2, 13, 0, 0,
                DateTimeKind.Utc),
            request.AdministeredAtUtc.UtcDateTime);
    }
}
