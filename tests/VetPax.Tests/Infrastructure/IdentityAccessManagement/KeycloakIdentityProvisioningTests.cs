
using System.Net;
using System.Net.Http;
using System.Text;
using Microsoft.Extensions.Options;
using VetPax.Infrastructure.Modules.IdentityAccessManagement.Adapters;
using VetPax.Infrastructure.Modules.IdentityAccessManagement.Configuration;
using Xunit;

namespace VetPax.Tests.Infrastructure.IdentityAccessManagement;

public sealed class KeycloakIdentityProvisioningTests
{
    [Fact]
    public async Task RegisterOwner_CreatesUserAndAssignsOwnerRole()
    {
        var userId = Guid.NewGuid();
        var calls = new List<string>();

        using var http = new HttpClient(
            new StubHttpMessageHandler(request =>
            {
                var path = request.RequestUri!.AbsolutePath;
                calls.Add($"{request.Method} {path}");

                if (path.EndsWith(
                        "/protocol/openid-connect/token") &&
                    request.Method == HttpMethod.Post)
                {
                    return Json(
                        """{"access_token":"admin-test-token"}""");
                }

                if (path.EndsWith("/roles/owner") &&
                    request.Method == HttpMethod.Get)
                {
                    return Json(
                        """{"id":"test-role-id","name":"owner"}""");
                }

                if (path.EndsWith("/admin/realms/vetpax/users") &&
                    request.Method == HttpMethod.Post)
                {
                    var response = new HttpResponseMessage(
                        HttpStatusCode.Created);

                    response.Headers.Location = new Uri(
                        $"http://localhost:8080/admin/realms/vetpax/users/{userId}");

                    return response;
                }

                if (path.EndsWith("/role-mappings/realm") &&
                    request.Method == HttpMethod.Post)
                {
                    return new HttpResponseMessage(
                        HttpStatusCode.NoContent);
                }

                return new HttpResponseMessage(
                    HttpStatusCode.NotFound);
            }));

        var adapter = new KeycloakIdentityProvisioningAdapter(
            http,
            CreateOptions());

        var result = await adapter.RegisterOwnerAsync(
            "owner@example.com",
            "ExamplePassword123!");

        Assert.Equal(userId, result.UserId);
        Assert.Equal("owner@example.com", result.Email);

        Assert.Contains(
            "POST /admin/realms/vetpax/users",
            calls);

        Assert.Contains(
            $"POST /admin/realms/vetpax/users/{userId}/role-mappings/realm",
            calls);
    }

    [Fact]
    public async Task DuplicateEmail_IsRejected()
    {
        using var http = new HttpClient(
            new StubHttpMessageHandler(request =>
            {
                var path = request.RequestUri!.AbsolutePath;

                if (path.EndsWith(
                    "/protocol/openid-connect/token"))
                {
                    return Json(
                        """{"access_token":"admin-test-token"}""");
                }

                if (path.EndsWith("/roles/owner"))
                {
                    return Json(
                        """{"id":"test-role-id","name":"owner"}""");
                }

                if (path.EndsWith("/admin/realms/vetpax/users"))
                {
                    return new HttpResponseMessage(
                        HttpStatusCode.Conflict);
                }

                return new HttpResponseMessage(
                    HttpStatusCode.NotFound);
            }));

        var adapter = new KeycloakIdentityProvisioningAdapter(
            http,
            CreateOptions());

        var exception = await Assert.ThrowsAsync<
            InvalidOperationException>(() =>
                adapter.RegisterOwnerAsync(
                    "existing@example.com",
                    "ExamplePassword123!"));

        Assert.Equal(
            "The email is already registered.",
            exception.Message);
    }

    private static IOptions<KeycloakOptions> CreateOptions()
    {
        return Options.Create(new KeycloakOptions
        {
            BaseUrl = "http://localhost:8080",
            Realm = "vetpax",
            Audience = "vetpax-api",
            AdminClientId = "vetpax-admin",
            AdminClientSecret = "test-only-secret"
        });
    }

    private static HttpResponseMessage Json(string content)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                content,
                Encoding.UTF8,
                "application/json")
        };
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<
            HttpRequestMessage,
            HttpResponseMessage> _respond;

        public StubHttpMessageHandler(
            Func<HttpRequestMessage, HttpResponseMessage> respond)
        {
            _respond = respond;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(_respond(request));
        }
    }
}
