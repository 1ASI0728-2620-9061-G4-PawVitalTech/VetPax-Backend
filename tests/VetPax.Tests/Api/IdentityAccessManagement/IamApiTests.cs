
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NSubstitute;
using VetPax.Api.Modules.IdentityAccessManagement.Controllers;
using VetPax.Api.Modules.IdentityAccessManagement.Resources;
using VetPax.Application.Common.Ports;
using VetPax.Application.Modules.IdentityAccessManagement.Ports;
using VetPax.Application.Modules.IdentityAccessManagement.UseCases.AuthenticateUser;
using VetPax.Application.Modules.IdentityAccessManagement.UseCases.RegisterAccount;
using VetPax.Domain.Modules.IdentityAccessManagement.ValueObjects;
using Xunit;

namespace VetPax.Tests.Api.IdentityAccessManagement;

public sealed class IamApiTests
{
    [Fact]
    public void AuthController_UsesExpectedRoute()
    {
        var route = typeof(AuthController)
            .GetCustomAttribute<RouteAttribute>();

        Assert.NotNull(route);
        Assert.Equal("api/v1/auth", route.Template);
    }

    [Fact]
    public void Registration_AllowsAnonymousRequests()
    {
        var method = typeof(AuthController)
            .GetMethod(nameof(AuthController.RegisterAsync));

        Assert.NotNull(method);

        Assert.NotNull(
            method.GetCustomAttribute<AllowAnonymousAttribute>());

        Assert.NotNull(
            method.GetCustomAttribute<EnableRateLimitingAttribute>());
    }

    [Fact]
    public void CurrentIdentity_RequiresAuthentication()
    {
        var method = typeof(AuthController)
            .GetMethod(nameof(AuthController.GetMeAsync));

        Assert.NotNull(method);

        Assert.NotNull(
            method.GetCustomAttribute<AuthorizeAttribute>());
    }

    [Fact]
    public void Permissions_RequireAuthentication()
    {
        var attribute = typeof(PermissionsController)
            .GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(attribute);
    }

    [Fact]
    public async Task Register_ReturnsCreatedResponse()
    {
        var provisioning =
            Substitute.For<IIdentityProvisioningPort>();

        var publisher =
            Substitute.For<IDomainEventPublisher>();

        var current =
            Substitute.For<IAuthenticatedUserPort>();

        var userId = Guid.NewGuid();

        provisioning.RegisterOwnerAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(new ProvisionedIdentity(
                userId,
                "owner@example.com"));

        var controller = new AuthController(
            new RegisterAccountHandler(
                provisioning,
                publisher),
            new AuthenticateUserHandler(current));

        var result = await controller.RegisterAsync(
            new RegisterAccountRequest(
                "owner@example.com",
                "ExamplePassword123!"),
            CancellationToken.None);

        var response = Assert.IsType<ObjectResult>(result);

        Assert.Equal(201, response.StatusCode);

        var body = Assert.IsType<RegisterAccountResponse>(
            response.Value);

        Assert.Equal(userId, body.UserId);
        Assert.Equal("owner", body.Role);
    }

    [Fact]
    public async Task DuplicateAccount_ReturnsConflict()
    {
        var provisioning =
            Substitute.For<IIdentityProvisioningPort>();

        provisioning.RegisterOwnerAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ProvisionedIdentity>(
                new InvalidOperationException(
                    "The email is already registered.")));

        var controller = new AuthController(
            new RegisterAccountHandler(
                provisioning,
                Substitute.For<IDomainEventPublisher>()),
            new AuthenticateUserHandler(
                Substitute.For<IAuthenticatedUserPort>()));

        var result = await controller.RegisterAsync(
            new RegisterAccountRequest(
                "owner@example.com",
                "ExamplePassword123!"),
            CancellationToken.None);

        var response = Assert.IsType<ObjectResult>(result);

        Assert.Equal(409, response.StatusCode);
    }

    [Fact]
    public async Task GetMe_ReturnsAuthenticatedUser()
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

        var controller = new AuthController(
            new RegisterAccountHandler(
                Substitute.For<IIdentityProvisioningPort>(),
                Substitute.For<IDomainEventPublisher>()),
            new AuthenticateUserHandler(current));

        var result = await controller.GetMeAsync(
            CancellationToken.None);

        var response = Assert.IsType<OkObjectResult>(result);

        var body = Assert.IsType<AuthenticatedUserResponse>(
            response.Value);

        Assert.Equal(userId, body.UserId);
        Assert.Equal("owner", body.Role);
    }
}
