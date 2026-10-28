
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetPax.Api.Modules.AdherenceGamification.Controllers;
using VetPax.Api.Modules.AdherenceGamification.Resources;
using VetPax.Application.Modules.AdherenceGamification.Models;
using Xunit;

namespace VetPax.Tests.Api.AdherenceGamification;

public sealed class AdherenceApiContractsTests
{
    [Fact]
    public void Controller_RequiresAuthentication()
    {
        var attribute = typeof(AdherenceController)
            .GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(attribute);
    }

    [Fact]
    public void Controller_UsesExpectedRoute()
    {
        var attribute = typeof(AdherenceController)
            .GetCustomAttribute<RouteAttribute>();

        Assert.NotNull(attribute);
        Assert.Equal(
            "api/v1/me/adherence",
            attribute.Template);
    }

    [Fact]
    public void GetEndpoint_UsesHttpGet()
    {
        var method = typeof(AdherenceController)
            .GetMethod(nameof(AdherenceController.GetAsync));

        Assert.NotNull(method);
        Assert.NotNull(
            method.GetCustomAttribute<HttpGetAttribute>());
    }

    [Fact]
    public void Response_MapsApplicationData()
    {
        var ownerId = Guid.NewGuid();
        var recognitionId = Guid.NewGuid();

        var data = new AdherenceData(
            ownerId,
            true,
            4,
            4m,
            "Plata",
            new[]
            {
                new RecognitionData(
                    recognitionId,
                    "Bronce",
                    "Plata",
                    new DateTime(
                        2026, 10, 23, 12, 0, 0,
                        DateTimeKind.Utc),
                    null)
            });

        var response = AdherenceResponse.From(data);

        Assert.Equal(ownerId, response.OwnerId);
        Assert.Equal("Plata", response.Level);
        Assert.Equal(4m, response.Score);

        var recognition = Assert.Single(
            response.Recognitions);

        Assert.Equal(
            recognitionId,
            recognition.RecognitionId);

        Assert.Equal("Bronce", recognition.FromLevel);
        Assert.Equal("Plata", recognition.ToLevel);
    }
}
