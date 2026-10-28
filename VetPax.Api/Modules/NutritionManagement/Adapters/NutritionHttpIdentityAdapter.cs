
using System.Security.Claims;
using System.Text.Json;
using VetPax.Application.Modules.NutritionManagement.Ports;

namespace VetPax.Api.Modules.NutritionManagement.Adapters;

public sealed class NutritionHttpIdentityAdapter
    : INutritionIdentityPort
{
    private readonly IHttpContextAccessor _context;

    public NutritionHttpIdentityAdapter(
        IHttpContextAccessor context)
    {
        _context = context;
    }

    public Task<NutritionActor?> GetCurrentAsync(
        CancellationToken cancellationToken = default)
    {
        var principal = _context.HttpContext?.User;

        if (principal?.Identity?.IsAuthenticated != true)
            return Task.FromResult<NutritionActor?>(null);

        var rawId =
            principal.FindFirst("sub")?.Value ??
            principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(rawId, out var userId) ||
            userId == Guid.Empty)
        {
            return Task.FromResult<NutritionActor?>(null);
        }

        var roles = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        void AddRoleValues(IEnumerable<string> values)
        {
            foreach (var rawRole in values)
            {
                if (string.IsNullOrWhiteSpace(rawRole))
                    continue;

                var normalizedRole = rawRole.Trim();
                if (normalizedRole.StartsWith("ROLE_",
                        StringComparison.OrdinalIgnoreCase))
                {
                    normalizedRole = normalizedRole[5..];
                }

                roles.Add(normalizedRole);
            }
        }

        foreach (var claim in principal.Claims)
        {
            if (claim.Type == ClaimTypes.Role ||
                claim.Type == "role" ||
                claim.Type == "roles")
            {
                if (claim.Value is { Length: > 0 })
                {
                    if (claim.Value.TrimStart().StartsWith("[",
                            StringComparison.Ordinal) ||
                        claim.Value.TrimStart().StartsWith("{",
                            StringComparison.Ordinal))
                    {
                        try
                        {
                            using var json = JsonDocument.Parse(claim.Value);
                            if (json.RootElement.ValueKind == JsonValueKind.Array)
                            {
                                AddRoleValues(
                                    json.RootElement.EnumerateArray()
                                        .Where(item => item.ValueKind == JsonValueKind.String)
                                        .Select(item => item.GetString() ?? string.Empty));
                            }
                            else if (json.RootElement.ValueKind == JsonValueKind.Object &&
                                     json.RootElement.TryGetProperty("roles", out var array) &&
                                     array.ValueKind == JsonValueKind.Array)
                            {
                                AddRoleValues(
                                    array.EnumerateArray()
                                        .Where(item => item.ValueKind == JsonValueKind.String)
                                        .Select(item => item.GetString() ?? string.Empty));
                            }
                        }
                        catch (JsonException)
                        {
                            // Ignore malformed role payloads.
                        }
                    }
                    else
                    {
                        AddRoleValues(claim.Value
                            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                    }
                }
            }
        }

        foreach (var claimName in new[]
                 {
                     "realm_access",
                     "resource_access"
                 })
        {
            var claimValue = principal.FindFirst(claimName)?.Value;
            if (string.IsNullOrWhiteSpace(claimValue))
                continue;

            try
            {
                using var json = JsonDocument.Parse(claimValue);
                if (json.RootElement.ValueKind != JsonValueKind.Object)
                    continue;

                foreach (var property in json.RootElement.EnumerateObject())
                {
                    if (!string.Equals(property.Name, "roles",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (property.Value.ValueKind != JsonValueKind.Array)
                        continue;

                    AddRoleValues(
                        property.Value.EnumerateArray()
                            .Where(item => item.ValueKind == JsonValueKind.String)
                            .Select(item => item.GetString() ?? string.Empty));
                }
            }
            catch (JsonException)
            {
                // An invalid realm/resource access claim grants no roles.
            }
        }

        var isOwner =
            principal.IsInRole("owner") ||
            roles.Contains("owner");

        var isVeterinarian =
            principal.IsInRole("veterinarian") ||
            roles.Contains("veterinarian");

        var role = (isOwner, isVeterinarian) switch
        {
            (true, false) => NutritionUserRole.Owner,
            (false, true) => NutritionUserRole.Veterinarian,
            _ => NutritionUserRole.Other
        };

        return Task.FromResult<NutritionActor?>(
            new NutritionActor(userId, role));
    }
}
