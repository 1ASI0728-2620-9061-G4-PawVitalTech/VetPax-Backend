using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.TestHost;
using VetPax.Application.Modules.IdentityAccessManagement.Ports;
using VetPax.Application.Modules.IdentityAccessManagement.UseCases.AuthenticateUser;
using VetPax.Domain.Modules.IdentityAccessManagement.ValueObjects;
using VetPax.Infrastructure.Persistence;
using VetPax.Tests.Acceptance.Support;
using Xunit;

namespace VetPax.Tests.Api.IdentityAccessManagement;

public sealed class IamHttpIntegrationTests : IClassFixture<IamHttpIntegrationTestFactory>
{
    private readonly HttpClient _client;

    public IamHttpIntegrationTests(IamHttpIntegrationTestFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }

    [Fact(Skip = "BLOCKED: requires VETPAX_TEST_MYSQL and a reachable Keycloak realm for IAM real HTTP validation.")]
    public async Task Register_Anonymous_Returns_Created_When_Environment_Is_Ready()
    {
        using var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new { email = "owner@example.com", password = "ExamplePassword123!" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact(Skip = "BLOCKED: requires JWT issuance/authentication flow and an IAM test user in Keycloak.")]
    public async Task GetMe_Without_Jwt_Returns_Unauthorized()
    {
        using var response = await _client.GetAsync("/api/v1/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact(Skip = "BLOCKED: requires a valid JWT for the request principal and the application auth middleware.")]
    public async Task GetPermissions_With_Authenticated_Owner_Returns_200()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me/permissions");
        request.Headers.Add("X-Test-User-Id", Guid.NewGuid().ToString());
        request.Headers.Add("X-Test-Role", "owner");

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

public sealed class IamHttpIntegrationTestFactory : WebApplicationFactory<global::VetPax.Api.ApiAssemblyMarker>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:VetPaxDb"] = Environment.GetEnvironmentVariable("VETPAX_TEST_MYSQL") ?? "Server=127.0.0.1;Database=vetpax_test_db;Uid=test;Pwd=test;",
                ["Keycloak:BaseUrl"] = "http://localhost:8080",
                ["Keycloak:Realm"] = "vetpax",
                ["Keycloak:Audience"] = "vetpax-api",
                ["Keycloak:AdminClientId"] = "vetpax-admin",
                ["Keycloak:AdminClientSecret"] = "bdd-test-only",
                ["Firebase:ProjectId"] = "vetpax-test-project"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            var connection = Environment.GetEnvironmentVariable("VETPAX_TEST_MYSQL")
                ?? "Server=127.0.0.1;Database=vetpax_test_db;Uid=test;Pwd=test;";

            services.RemoveAll<DbContextOptions<VetPaxDbContext>>();
            services.AddDbContext<VetPaxDbContext>(options => options.UseMySql(
                connection,
                new MySqlServerVersion(new Version(8, 0, 0))));

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.Scheme;
                options.DefaultChallengeScheme = TestAuthHandler.Scheme;
                options.DefaultForbidScheme = TestAuthHandler.Scheme;
            }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.Scheme, _ => { });

            services.RemoveAll<IIdentityProvisioningPort>();
            services.RemoveAll<IAuthenticatedUserPort>();

            services.AddScoped<IIdentityProvisioningPort, FakeIdentityProvisioningPort>();
            services.AddScoped<IAuthenticatedUserPort, FakeAuthenticatedUserPort>();
        });
    }

    private sealed class FakeIdentityProvisioningPort : IIdentityProvisioningPort
    {
        public Task<ProvisionedIdentity> RegisterOwnerAsync(string firstName, string lastName, string email, string password, CancellationToken cancellationToken = default)
            => Task.FromResult(new ProvisionedIdentity(Guid.Parse("11111111-1111-1111-1111-111111111111"), email));
    }

    private sealed class FakeAuthenticatedUserPort : IAuthenticatedUserPort
    {
        public Task<AuthenticatedIdentity?> GetCurrentAsync(CancellationToken cancellationToken = default)
        {
            var userId = Guid.TryParse(Environment.GetEnvironmentVariable("VETPAX_TEST_USER_ID"), out var parsed)
                ? parsed
                : Guid.Parse("22222222-2222-2222-2222-222222222222");

            return Task.FromResult<AuthenticatedIdentity?>(new AuthenticatedIdentity(
                new UserId(userId),
                "owner@example.com",
                UserRole.Owner));
        }
    }
}
