
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Reqnroll;
using Xunit;

namespace VetPax.Tests.Features.MedicationTreatment;

[Binding]
public sealed class MedicationTreatmentApiSteps
{
    private HttpClient? _client;
    private HttpResponseMessage? _response;
    private Guid? _createdPlanId;

    [Given(
        @"^the VetPax Medication E2E environment is configured$")]
    public void GivenEnvironmentIsConfigured()
    {
        var url = Required("VETPAX_E2E_BASE_URL");

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            throw new InvalidOperationException(
                "VETPAX_E2E_BASE_URL is invalid.");

        var local = uri.Host.Equals(
                        "localhost",
                        StringComparison.OrdinalIgnoreCase)
                    || (IPAddress.TryParse(uri.Host, out var ip)
                        && IPAddress.IsLoopback(ip));

        if (!local)
            throw new InvalidOperationException(
                "E2E tests must use a locally running API.");

        if (Environment.GetEnvironmentVariable(
                "VETPAX_E2E_TARGET_DB") != "vetpax_test_db")
        {
            throw new InvalidOperationException(
                "Confirm vetpax_test_db before running E2E tests.");
        }

        _client = new HttpClient
        {
            BaseAddress = uri
        };
    }

    [Given(
        @"^the test veterinarian creates a medication plan$")]
    public async Task GivenVeterinarianCreatesPlan()
    {
        var petId = RequiredGuid("VETPAX_E2E_PET_ID");
        var token = Required("VETPAX_E2E_VET_TOKEN");

        using var response = await SendAsync(
            HttpMethod.Post,
            $"api/v1/pets/{petId}/medication-plans",
            CreatePlanBody(),
            token);

        var body = await response.Content.ReadAsStringAsync();

        Assert.True(
            response.StatusCode == HttpStatusCode.Created,
            $"Expected 201, received {(int)response.StatusCode}. " +
            body);

        using var json = JsonDocument.Parse(body);

        Assert.True(
            json.RootElement.TryGetProperty(
                "planId",
                out var planIdElement),
            "The API response does not contain planId.");

        _createdPlanId = planIdElement.GetGuid();
    }

    [When(
        @"^the test owner sets medication reminders to ""(true|false)""$")]
    public async Task WhenOwnerConfiguresReminders(
        string enabledValue)
    {
        Assert.True(
            _createdPlanId.HasValue,
            "A medication plan must be created first.");

        var enabled = bool.Parse(enabledValue);

        await SendAndStoreAsync(
            HttpMethod.Patch,
            $"api/v1/medication-plans/{_createdPlanId}/reminders",
            new { enabled },
            Required("VETPAX_E2E_OWNER_TOKEN"));
    }

    [When(
        @"^a user without JWT requests a medication plan$")]
    public async Task WhenUnauthenticatedUserRequestsPlan()
    {
        await SendAndStoreAsync(
            HttpMethod.Get,
            $"api/v1/medication-plans/{Guid.NewGuid()}",
            null,
            null);
    }

    [When(
        @"^the test owner attempts to create a medication plan$")]
    public async Task WhenOwnerAttemptsToCreatePlan()
    {
        var petId = RequiredGuid("VETPAX_E2E_PET_ID");

        await SendAndStoreAsync(
            HttpMethod.Post,
            $"api/v1/pets/{petId}/medication-plans",
            CreatePlanBody(),
            Required("VETPAX_E2E_OWNER_TOKEN"));
    }

    [When(
        @"^the test owner registers a dose without a timestamp$")]
    public async Task WhenOwnerOmitsAdministrationTimestamp()
    {
        await SendAndStoreAsync(
            HttpMethod.Post,
            $"api/v1/medication-doses/{Guid.NewGuid()}/administrations",
            new { },
            Required("VETPAX_E2E_OWNER_TOKEN"));
    }

    [Then(
        @"^the medication response status is (\d+)$")]
    public async Task ThenStatusIs(int expectedStatus)
    {
        Assert.NotNull(_response);

        var body = await _response.Content.ReadAsStringAsync();

        Assert.True(
            (int)_response.StatusCode == expectedStatus,
            $"Expected HTTP {expectedStatus}, " +
            $"received HTTP {(int)_response.StatusCode}. " +
            body);
    }

    [Then(
        @"^medication reminders are ""(true|false)"" in the response$")]
    public async Task ThenReminderFlagIs(
        string expectedValue)
    {
        Assert.NotNull(_response);

        var body = await _response.Content.ReadAsStringAsync();

        using var json = JsonDocument.Parse(body);

        Assert.True(
            json.RootElement.TryGetProperty(
                "remindersEnabled",
                out var property),
            "Response does not contain remindersEnabled.");

        Assert.Equal(
            bool.Parse(expectedValue),
            property.GetBoolean());
    }

    [AfterScenario("medication_e2e")]
    public async Task CleanupAsync()
    {
        // Only cancel plans created by this test scenario.
        if (_client is not null && _createdPlanId.HasValue)
        {
            try
            {
                using var response = await SendAsync(
                    HttpMethod.Patch,
                    $"api/v1/medication-plans/{_createdPlanId}",
                    new { operation = "cancel" },
                    Required("VETPAX_E2E_VET_TOKEN"));

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine(
                        "E2E cleanup could not cancel plan " +
                        _createdPlanId);
                }
            }
            catch (Exception exception)
            {
                // Cleanup failure must be visible without
                // concealing the original test failure.
                Console.WriteLine(
                    "E2E cleanup failed: " +
                    exception.GetType().Name);
            }
        }

        _response?.Dispose();
        _client?.Dispose();
    }

    private async Task SendAndStoreAsync(
        HttpMethod method,
        string path,
        object? body,
        string? jwt)
    {
        _response?.Dispose();

        _response = await SendAsync(
            method,
            path,
            body,
            jwt);
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string path,
        object? body,
        string? jwt)
    {
        if (_client is null)
            throw new InvalidOperationException(
                "E2E HttpClient has not been initialized.");

        using var request = new HttpRequestMessage(
            method,
            path);

        if (body is not null)
            request.Content = JsonContent.Create(body);

        if (!string.IsNullOrWhiteSpace(jwt))
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    jwt);
        }

        return await _client.SendAsync(request);
    }

    private static object CreatePlanBody()
    {
        var startDate = DateOnly
            .FromDateTime(DateTime.UtcNow)
            .AddDays(1);

        var endDate = startDate.AddDays(4);

        var attentionValue = Environment.GetEnvironmentVariable(
            "VETPAX_E2E_ATTENTION_ID");

        Guid? attentionId =
            Guid.TryParse(attentionValue, out var parsed)
                ? parsed
                : null;

        return new
        {
            clinicId = RequiredGuid("VETPAX_E2E_CLINIC_ID"),
            clinicalAttentionId = attentionId,
            medicationName = "VetPax E2E Test Medication",
            route = "Oral",
            instructions = "Automated integration test",
            startDate = startDate.ToString("yyyy-MM-dd"),
            endDate = endDate.ToString("yyyy-MM-dd"),
            timeZoneId = "America/Lima",
            schedules = new[]
            {
                new
                {
                    localTime = "08:00:00",
                    daysOfWeek = Enum.GetNames<DayOfWeek>(),
                    amount = 1m,
                    unit = "tablet"
                }
            }
        };
    }

    private static Guid RequiredGuid(string variable)
    {
        var value = Required(variable);

        if (!Guid.TryParse(value, out var id) ||
            id == Guid.Empty)
        {
            throw new InvalidOperationException(
                $"Invalid GUID in {variable}.");
        }

        return id;
    }

    private static string Required(string variable)
    {
        var value = Environment.GetEnvironmentVariable(
            variable);

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Environment variable {variable} is required.");
        }

        return value;
    }
}
