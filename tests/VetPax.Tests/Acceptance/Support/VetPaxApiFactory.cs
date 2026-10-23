
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MySqlConnector;
using VetPax.Infrastructure.Persistence;

namespace VetPax.Tests.Acceptance.Support;

public sealed class VetPaxApiFactory
    : WebApplicationFactory<global::VetPax.Api.ApiAssemblyMarker>
{
    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        var connection = Environment.GetEnvironmentVariable(
            "VETPAX_TEST_MYSQL")
            ?? throw new InvalidOperationException(
                "Configure VETPAX_TEST_MYSQL.");

        var database = new MySqlConnectionStringBuilder(
            connection).Database;

        if (!string.Equals(
            database,
            "vetpax_test_db",
            StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Only vetpax_test_db is allowed.");
        }

        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:VetPaxDb"] = connection,
                    ["Keycloak:BaseUrl"] = "http://localhost:8080",
                    ["Keycloak:Realm"] = "vetpax",
                    ["Keycloak:Audience"] = "vetpax-api",
                    ["Keycloak:AdminClientId"] = "vetpax-admin",
                    ["Keycloak:AdminClientSecret"] = "bdd-test-only"
                });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<
                DbContextOptions<VetPaxDbContext>>();

            services.AddDbContext<VetPaxDbContext>(
                options => options.UseMySql(
                    connection,
                    new MySqlServerVersion(
                        new Version(8, 0, 0))));

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme =
                    TestAuthHandler.Scheme;
                options.DefaultChallengeScheme =
                    TestAuthHandler.Scheme;
                options.DefaultForbidScheme =
                    TestAuthHandler.Scheme;
            })
            .AddScheme<
                AuthenticationSchemeOptions,
                TestAuthHandler>(
                TestAuthHandler.Scheme,
                _ => { });
        });
    }
}
