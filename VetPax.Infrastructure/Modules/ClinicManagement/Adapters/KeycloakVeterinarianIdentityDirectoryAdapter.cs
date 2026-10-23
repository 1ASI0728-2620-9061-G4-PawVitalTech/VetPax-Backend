
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using VetPax.Application.Modules.ClinicManagement.Ports;
using VetPax.Infrastructure.Modules.IdentityAccessManagement.Configuration;

namespace VetPax.Infrastructure.Modules.ClinicManagement.Adapters;

public sealed class KeycloakVeterinarianIdentityDirectoryAdapter(
    HttpClient http,
    IOptions<KeycloakOptions> options)
    : IVeterinarianIdentityDirectoryPort
{
    public async Task<bool> IsActiveVeterinarianAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            return false;

        var config = options.Value;

        var adminBase =
            $"{config.BaseUrl.TrimEnd('/')}/admin/realms/" +
            Uri.EscapeDataString(config.Realm);

        using var credentials = new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = config.AdminClientId,
                ["client_secret"] = config.AdminClientSecret
            });

        using var tokenResponse = await http.PostAsync(
            $"{config.Authority}/protocol/openid-connect/token",
            credentials,
            cancellationToken);

        tokenResponse.EnsureSuccessStatusCode();

        using var tokenJson = await JsonDocument.ParseAsync(
            await tokenResponse.Content.ReadAsStreamAsync(
                cancellationToken),
            cancellationToken: cancellationToken);

        var token = tokenJson.RootElement
            .GetProperty("access_token")
            .GetString()!;

        using var userRequest = AuthenticatedGet(
            $"{adminBase}/users/{userId}", token);

        using var userResponse = await http.SendAsync(
            userRequest, cancellationToken);

        if (userResponse.StatusCode == HttpStatusCode.NotFound)
            return false;

        userResponse.EnsureSuccessStatusCode();

        using var user = await JsonDocument.ParseAsync(
            await userResponse.Content.ReadAsStreamAsync(
                cancellationToken),
            cancellationToken: cancellationToken);

        if (!user.RootElement.TryGetProperty(
                "enabled", out var enabled) ||
            enabled.ValueKind != JsonValueKind.True)
            return false;

        using var rolesRequest = AuthenticatedGet(
            $"{adminBase}/users/{userId}/role-mappings/realm/composite",
            token);

        using var rolesResponse = await http.SendAsync(
            rolesRequest, cancellationToken);

        rolesResponse.EnsureSuccessStatusCode();

        var roles = await rolesResponse.Content
            .ReadFromJsonAsync<JsonElement>(
                cancellationToken: cancellationToken);

        if (roles.ValueKind != JsonValueKind.Array)
            return false;

        var names = roles.EnumerateArray()
            .Where(x =>
                x.TryGetProperty("name", out var name) &&
                name.ValueKind == JsonValueKind.String)
            .Select(x => x.GetProperty("name").GetString())
            .ToHashSet(StringComparer.Ordinal);

        return names.Contains("veterinarian") &&
               !names.Contains("owner") &&
               !names.Contains("clinic_administrator");
    }

    private static HttpRequestMessage AuthenticatedGet(
        string url,
        string token)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Get, url);

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        return request;
    }
}
