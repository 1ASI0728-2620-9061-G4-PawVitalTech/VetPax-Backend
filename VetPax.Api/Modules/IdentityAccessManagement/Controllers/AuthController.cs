
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using VetPax.Api.Modules.IdentityAccessManagement.Resources;
using VetPax.Application.Modules.IdentityAccessManagement.UseCases.AuthenticateUser;
using VetPax.Application.Modules.IdentityAccessManagement.UseCases.RegisterAccount;

namespace VetPax.Api.Modules.IdentityAccessManagement.Controllers;

[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public sealed class AuthController : ControllerBase
{
    private readonly RegisterAccountHandler _register;
    private readonly AuthenticateUserHandler _authenticate;

    public AuthController(
        RegisterAccountHandler register,
        AuthenticateUserHandler authenticate)
    {
        _register = register;
        _authenticate = authenticate;
    }

    /// <summary>
    /// Registers a new pet owner in Keycloak.
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("iam-registration")]
    [ProducesResponseType(
        typeof(RegisterAccountResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    [ProducesResponseType(
        StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(
        StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> RegisterAsync(
        [FromBody] RegisterAccountRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new RegisterAccountCommand(
                request.Email,
                request.Password);

            var result = await _register.HandleAsync(
                command,
                cancellationToken);

            return StatusCode(
                StatusCodes.Status201Created,
                RegisterAccountResponse.From(result));
        }
        catch (ArgumentException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid registration data",
                detail: ex.Message);
        }
        catch (InvalidOperationException ex)
            when (ex.Message == "The email is already registered.")
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Account already registered",
                detail: "An account with this email already exists.");
        }
        catch (HttpRequestException)
        {
            return Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "Identity provider unavailable",
                detail: "The account could not be registered.");
        }
        catch (JsonException)
        {
            return Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "Invalid identity provider response");
        }
    }

    /// <summary>
    /// Returns the identity of the authenticated user.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(
        typeof(AuthenticatedUserResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMeAsync(
        CancellationToken cancellationToken)
    {
        var result = await _authenticate.HandleAsync(
            cancellationToken);

        if (result is null)
        {
            // The JWT was authenticated, but IAM could
            // not map its required identity claims.
            return Forbid();
        }

        return Ok(
            AuthenticatedUserResponse.From(result));
    }
}
