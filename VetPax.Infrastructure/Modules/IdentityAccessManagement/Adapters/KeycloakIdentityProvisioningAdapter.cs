
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using VetPax.Application.Modules.IdentityAccessManagement.Ports;
using VetPax.Infrastructure.Modules.IdentityAccessManagement.Configuration;

namespace VetPax.Infrastructure.Modules.IdentityAccessManagement.Adapters;

public sealed class KeycloakIdentityProvisioningAdapter(
    HttpClient http,
    IOptions<KeycloakOptions> options
) : IIdentityProvisioningPort
{
    public async Task<ProvisionedIdentity> RegisterOwnerAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var config = options.Value;
        var adminUrl =
            $"{config.BaseUrl.TrimEnd('/')}/admin/realms/{config.Realm}";

        using var tokenBody = new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = config.AdminClientId,
                ["client_secret"] = config.AdminClientSecret
            });

        using var tokenResponse = await http.PostAsync(
            $"{config.Authority}/protocol/openid-connect/token",
            tokenBody, cancellationToken);

        tokenResponse.EnsureSuccessStatusCode();

        using var tokenJson = JsonDocument.Parse(
            await tokenResponse.Content.ReadAsStringAsync(
                cancellationToken));

        var token = tokenJson.RootElement
            .GetProperty("access_token").GetString()!;

        // Obtain the owner role before creating the account
        using var roleRequest = new HttpRequestMessage(
            HttpMethod.Get, $"{adminUrl}/roles/owner");

        roleRequest.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        using var roleResponse = await http.SendAsync(
            roleRequest, cancellationToken);

        roleResponse.EnsureSuccessStatusCode();

        var role = await roleResponse.Content
            .ReadFromJsonAsync<JsonElement>(
                cancellationToken: cancellationToken);

        using var createRequest = new HttpRequestMessage(
            HttpMethod.Post, $"{adminUrl}/users");

        createRequest.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        createRequest.Content = JsonContent.Create(new
        {
            username = email.Trim(),
            email = email.Trim(),
            enabled = true,
            emailVerified = false,
            credentials = new[]
            {
                new
                {
                    type = "password",
                    value = password,
                    temporary = false
                }
            }
        });

        using var created = await http.SendAsync(
            createRequest, cancellationToken);

        if (created.StatusCode == HttpStatusCode.Conflict)
            throw new InvalidOperationException(
                "The email is already registered.");

        created.EnsureSuccessStatusCode();

        var idText = created.Headers.Location?
            .Segments.LastOrDefault()?.TrimEnd('/');

        if (!Guid.TryParse(idText, out var userId))
            throw new InvalidOperationException(
                "Keycloak did not return a valid user ID.");

        var userUrl = $"{adminUrl}/users/{userId}";

        try
        {
            using var assignRequest = new HttpRequestMessage(
                HttpMethod.Post,
                $"{userUrl}/role-mappings/realm");

            assignRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            assignRequest.Content = JsonContent.Create(
                new[] { role });

            using var assigned = await http.SendAsync(
                assignRequest, cancellationToken);

            assigned.EnsureSuccessStatusCode();
        }
        catch
        {
            // Best-effort compensation if role assignment fails
            try
            {
                using var deleteRequest = new HttpRequestMessage(
                    HttpMethod.Delete, userUrl);

                deleteRequest.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

                using var deleted = await http.SendAsync(
                    deleteRequest, CancellationToken.None);
            }
            catch (HttpRequestException)
            {
                // Compensation failure must be logged in production.
            }

            throw;
        }

        return new ProvisionedIdentity(userId, email.Trim());
    }
}
