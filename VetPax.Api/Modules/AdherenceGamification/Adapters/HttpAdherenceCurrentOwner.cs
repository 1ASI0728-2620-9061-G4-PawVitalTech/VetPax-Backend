
using System.Security.Claims;
using System.Text.Json;
using VetPax.Application.Modules.AdherenceGamification.Ports;

namespace VetPax.Api.Modules.AdherenceGamification.Adapters;

public sealed class HttpAdherenceCurrentOwner : IAdherenceCurrentOwner
{
    private readonly IHttpContextAccessor _accessor;

    public HttpAdherenceCurrentOwner(
        IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public Task<Guid> RequireOwnerIdAsync(
        CancellationToken cancellationToken = default)
    {
        var user = _accessor.HttpContext?.User;

        if (user?.Identity?.IsAuthenticated != true)
            throw new UnauthorizedAccessException(
                "Authentication is required.");

        var rawId =
            user.FindFirst("sub")?.Value ??
            user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(rawId, out var ownerId) ||
            ownerId == Guid.Empty)
        {
            throw new UnauthorizedAccessException(
                "A valid authenticated owner ID is required.");
        }

        var roles = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var claim in user.Claims)
        {
            if (claim.Type == "role" ||
                claim.Type == "roles" ||
                claim.Type == ClaimTypes.Role)
            {
                roles.Add(claim.Value);
            }
        }

        var realmAccess =
            user.FindFirst("realm_access")?.Value;

        if (!string.IsNullOrWhiteSpace(realmAccess))
        {
            try
            {
                using var json = JsonDocument.Parse(realmAccess);

                if (json.RootElement.TryGetProperty(
                        "roles", out var values) &&
                    values.ValueKind == JsonValueKind.Array)
                {
                    foreach (var value in values.EnumerateArray())
                    {
                        if (value.ValueKind ==
                                JsonValueKind.String &&
                            value.GetString() is { } role)
                        {
                            roles.Add(role);
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // Invalid role claims grant no permissions.
            }
        }

        var isOwner =
            user.IsInRole("owner") ||
            roles.Contains("owner");

        if (!isOwner)
            throw new UnauthorizedAccessException(
                "The owner role is required.");

        return Task.FromResult(ownerId);
    }
}
