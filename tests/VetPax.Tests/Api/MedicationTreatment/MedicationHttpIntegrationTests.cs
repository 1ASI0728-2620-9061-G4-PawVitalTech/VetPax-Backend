using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using VetPax.Application.Modules.IdentityAccessManagement.Ports;
using VetPax.Application.Modules.MedicationTreatment.Models;
using VetPax.Application.Modules.MedicationTreatment.Ports;
using VetPax.Application.Modules.MedicationTreatment.Services;
using VetPax.Domain.Modules.IdentityAccessManagement.ValueObjects;
using VetPax.Domain.Modules.MedicationTreatment.Aggregates;
using VetPax.Domain.Modules.MedicationTreatment.Entities;
using VetPax.Domain.Modules.MedicationTreatment.ValueObjects;
using VetPax.Domain.Shared.Events;
using VetPax.Infrastructure.Persistence;
using VetPax.Tests.Acceptance.Support;
using Xunit;

namespace VetPax.Tests.Api.MedicationTreatment;

public sealed class MedicationHttpIntegrationTests : IClassFixture<MedicationHttpIntegrationTestFactory>
{
    private readonly HttpClient _client;

    public MedicationHttpIntegrationTests(MedicationHttpIntegrationTestFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }

    [Fact]
    public async Task GetByPet_WithoutAuth_Returns_401()
    {
        using var response = await _client.GetAsync($"/api/v1/pets/{Guid.NewGuid()}/medication-plans");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetByPet_WithOwnerRole_Returns_200_Or_404()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/pets/{Guid.NewGuid()}/medication-plans");
        request.Headers.Add("X-Test-User-Id", Guid.NewGuid().ToString());
        request.Headers.Add("X-Test-Role", "owner");

        using var response = await _client.SendAsync(request);

        Assert.True(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.NotFound,
            $"Expected 200 or 404 but got {(int)response.StatusCode}");
    }

    [Fact]
    public async Task GetByPlanId_WithoutAuth_Returns_401()
    {
        using var response = await _client.GetAsync($"/api/v1/medication-plans/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact(Skip = "BLOCKED: needs a valid authenticated user, DB records, and non-empty clinic/veterinarian linkage to create a real plan.")]
    public async Task CreatePlan_With_Veterinarian_Returns_Created()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/pets/{Guid.NewGuid()}/medication-plans");
        request.Headers.Add("X-Test-User-Id", Guid.NewGuid().ToString());
        request.Headers.Add("X-Test-Role", "veterinarian");
        request.Content = JsonContent.Create(new
        {
            clinicId = Guid.NewGuid(),
            clinicalAttentionId = (Guid?)null,
            medicationName = "Amoxicillin",
            route = "Oral",
            instructions = "Take once daily",
            startDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1).ToString("yyyy-MM-dd"),
            endDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(4).ToString("yyyy-MM-dd"),
            timeZoneId = "UTC",
            schedules = new[]
            {
                new { localTime = "08:00:00", daysOfWeek = new[] { "Monday", "Wednesday", "Friday" }, amount = 1m, unit = "tablet" }
            }
        });

        using var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}

public sealed class MedicationHttpIntegrationTestFactory : WebApplicationFactory<global::VetPax.Api.ApiAssemblyMarker>
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

            services.RemoveAll<IAuthenticatedUserPort>();
            services.RemoveAll<IMedicationPatientPort>();
            services.RemoveAll<IMedicationVeterinarianPort>();
            services.RemoveAll<IMedicationClinicalContextPort>();
            services.RemoveAll<IMedicationReminderPort>();
            services.RemoveAll<IMedicationOutboxPort>();
            services.RemoveAll<IMedicationNotificationSender>();

            services.AddSingleton<IAuthenticatedUserPort, FakeAuthenticatedUserPort>();
            services.AddSingleton<IMedicationPatientPort, FakeMedicationPatientPort>();
            services.AddSingleton<IMedicationVeterinarianPort, FakeMedicationVeterinarianPort>();
            services.AddSingleton<IMedicationClinicalContextPort, FakeMedicationClinicalContextPort>();
            services.AddSingleton<IMedicationReminderPort, FakeMedicationReminderPort>();
            services.AddSingleton<IMedicationOutboxPort, FakeMedicationOutboxPort>();
            services.AddSingleton<IMedicationNotificationSender, FakeMedicationNotificationSender>();
        });
    }

    private sealed class FakeAuthenticatedUserPort : IAuthenticatedUserPort
    {
        public Task<AuthenticatedIdentity?> GetCurrentAsync(CancellationToken cancellationToken = default)
        {
            var userId = Guid.TryParse(Environment.GetEnvironmentVariable("VETPAX_TEST_USER_ID"), out var parsed)
                ? parsed
                : Guid.Parse("33333333-3333-3333-3333-333333333333");

            return Task.FromResult<AuthenticatedIdentity?>(new AuthenticatedIdentity(
                new UserId(userId),
                "owner@example.com",
                UserRole.Owner));
        }
    }

    private sealed class FakeMedicationPatientPort : IMedicationPatientPort
    {
        public Task<Guid?> GetOwnerIdAsync(Guid petId, CancellationToken cancellationToken = default)
            => Task.FromResult<Guid?>(Guid.Parse("44444444-4444-4444-4444-444444444444"));
    }

    private sealed class FakeMedicationVeterinarianPort : IMedicationVeterinarianPort
    {
        public Task<bool> CanPrescribeAsync(Guid veterinarianId, Guid clinicId, Guid petId, CancellationToken cancellationToken = default)
            => Task.FromResult(true);
    }

    private sealed class FakeMedicationClinicalContextPort : IMedicationClinicalContextPort
    {
        public Task<bool> MatchesAsync(Guid clinicalAttentionId, Guid petId, Guid clinicId, Guid veterinarianId, CancellationToken cancellationToken = default)
            => Task.FromResult(true);
    }

    private sealed class FakeMedicationReminderPort : IMedicationReminderPort
    {
        public Task StageForDoseAsync(MedicationPlan plan, MedicationDose dose, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CancelForDoseAsync(Guid doseId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CancelPendingForPlanAsync(Guid planId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeMedicationOutboxPort : IMedicationOutboxPort
    {
        public Task StageAsync(IReadOnlyCollection<IDomainEvent> events, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeMedicationNotificationSender : IMedicationNotificationSender
    {
        public Task SendAsync(MedicationNotificationMessage message, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
