
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetPax.Api.Modules.NutritionManagement.Controllers;
using VetPax.Api.Modules.NutritionManagement.Resources;
using VetPax.Domain.Modules.NutritionManagement.Exceptions;
using Xunit;

namespace VetPax.Tests.Api.NutritionManagement;

public sealed class NutritionApiContractsTests
{
    private static readonly Guid PetId = Guid.NewGuid();
    private static readonly Guid ClinicId = Guid.NewGuid();

    [Fact]
    public void CreateRequest_MapsToApplicationCommand()
    {
        var request = new CreateFeedingPlanRequest(
            ClinicId,
            "Veterinary food",
            "Prescribed by veterinarian",
            new DateOnly(2026, 11, 1),
            new DateOnly(2026, 11, 30),
            "America/Lima",
            new[]
            {
                new FeedingScheduleRequest(
                    new TimeOnly(8, 0),
                    new[] { "monday", "FRIDAY" },
                    150m,
                    "g")
            });

        var command = FeedingPlanRequestMapper.ToCommand(
            PetId,
            request);

        Assert.Equal(PetId, command.PetId);
        Assert.Equal(ClinicId, command.ClinicId);
        Assert.Equal("Veterinary food", command.FoodName);

        var schedule = Assert.Single(command.Schedules);

        Assert.Equal(new TimeOnly(8, 0), schedule.LocalTime);
        Assert.Equal(150m, schedule.Quantity);

        Assert.Contains(
            DayOfWeek.Monday,
            schedule.DaysOfWeek);

        Assert.Contains(
            DayOfWeek.Friday,
            schedule.DaysOfWeek);
    }

    [Theory]
    [InlineData("Funday")]
    [InlineData("8")]
    [InlineData("-1")]
    [InlineData("")]
    public void InvalidWeekday_IsRejected(string day)
    {
        var schedules = new[]
        {
            new FeedingScheduleRequest(
                new TimeOnly(8, 0),
                new[] { day },
                150m,
                "g")
        };

        Assert.Throws<NutritionDomainException>(() =>
            FeedingPlanRequestMapper.ToScheduleInputs(schedules));
    }

    [Fact]
    public void EmptySchedules_AreRejected()
    {
        Assert.Throws<NutritionDomainException>(() =>
            FeedingPlanRequestMapper.ToScheduleInputs(
                Array.Empty<FeedingScheduleRequest>()));
    }

    [Fact]
    public void MissingScheduleDays_AreRejected()
    {
        var schedules = new[]
        {
            new FeedingScheduleRequest(
                new TimeOnly(8, 0),
                Array.Empty<string>(),
                150m,
                "g")
        };

        Assert.Throws<NutritionDomainException>(() =>
            FeedingPlanRequestMapper.ToScheduleInputs(schedules));
    }

    [Fact]
    public void PetFeedingPlansController_RequiresAuthentication()
    {
        Assert.NotNull(
            typeof(PetFeedingPlansController)
                .GetCustomAttribute<AuthorizeAttribute>());
    }

    [Fact]
    public void FeedingPlansController_RequiresAuthentication()
    {
        Assert.NotNull(
            typeof(FeedingPlansController)
                .GetCustomAttribute<AuthorizeAttribute>());
    }

    [Fact]
    public void GetByIdRoute_HasConsistentName()
    {
        var method = typeof(FeedingPlansController)
            .GetMethod(
                nameof(FeedingPlansController.GetByIdAsync));

        Assert.NotNull(method);

        var attribute =
            method.GetCustomAttribute<HttpGetAttribute>();

        Assert.NotNull(attribute);
        Assert.Equal(
            FeedingPlansController.GetByIdRouteName,
            attribute.Name);
    }
}
