using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace VetPax.Tests.Acceptance.Support;

public sealed class TestAuthHandler
    : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string Scheme = "BddTest";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var idText = Request.Headers["X-Test-User-Id"]
            .FirstOrDefault();

        var role = Request.Headers["X-Test-Role"]
            .FirstOrDefault();

        if (string.IsNullOrEmpty(idText) &&
            string.IsNullOrEmpty(role))
        {
            return Task.FromResult(
                AuthenticateResult.NoResult());
        }

        if (!Guid.TryParse(idText, out var userId) ||
            userId == Guid.Empty ||
            role is not ("owner" or "veterinarian"
                or "clinic_administrator"))
        {
            return Task.FromResult(
                AuthenticateResult.Fail(
                    "Invalid test identity."));
        }

        var claims = new[]
        {
            new Claim("sub", userId.ToString()),
            new Claim("email", $"{userId}@test.vetpax.local"),
            new Claim(ClaimTypes.Role, role)
        };

        var identity = new ClaimsIdentity(
            claims, Scheme);

        var principal = new ClaimsPrincipal(identity);

        var ticket = new AuthenticationTicket(
            principal, Scheme);

        return Task.FromResult(
            AuthenticateResult.Success(ticket));
    }
}