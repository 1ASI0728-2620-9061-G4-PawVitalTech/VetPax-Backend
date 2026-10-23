
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;
using Xunit;
using VetPax.Domain.Modules.PetClinicalCare.ValueObjects;
using VetPax.Infrastructure.Persistence;
using VetPax.Tests.Acceptance.Support;

namespace VetPax.Tests.Acceptance.Steps;

[Binding]
public sealed class PetClinicalCareSteps
{
    private VetPaxApiFactory _factory = null!;
    private HttpClient _client = null!;
    private HttpResponseMessage? _response;

    private Guid _ownerId;
    private Guid _petId;
    private string? _role;

    [BeforeScenario("pet")]
    public void Setup()
    {
        _factory = new VetPaxApiFactory();

        _client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                AllowAutoRedirect = false
            });
    }

    [Given("que soy un propietario nuevo")]
    public void GivenOwner()
    {
        _ownerId = Guid.NewGuid();
        _role = "owner";
    }

    [Given("que no estoy autenticado")]
    public void GivenAnonymous()
    {
        _ownerId = Guid.Empty;
        _role = null;
    }

    [Given("que soy un veterinario autenticado")]
    public void GivenVeterinarian()
    {
        _ownerId = Guid.NewGuid();
        _role = "veterinarian";
    }

    [Given("que ya registré una mascota")]
    public async Task GivenRegisteredPet()
    {
        GivenOwner();
        await RegisterAsync("Luna");

        Assert.Equal(
            HttpStatusCode.Created,
            _response!.StatusCode);
    }

    [When("registro una mascota válida")]
    public Task WhenRegisterValid()
        => RegisterAsync("Luna");

    [When("intento registrar una mascota sin nombre")]
    public Task WhenRegisterInvalid()
        => RegisterAsync("");

    [When("consulto su historial como propietario")]
    public Task WhenOwnerGetsHistory()
        => GetHistoryAsync(_ownerId);

    [When("otro propietario consulta su historial")]
    public Task WhenStrangerGetsHistory()
        => GetHistoryAsync(Guid.NewGuid());

    [Then(@"la respuesta HTTP es (\d+)")]
    public void ThenStatusCode(int expected)
    {
        Assert.Equal(
            expected,
            (int)_response!.StatusCode);
    }

    [Then("se crea un historial clínico vacío")]
    public async Task ThenRecordCreated()
    {
        Assert.NotEqual(Guid.Empty, _petId);

        await GetHistoryAsync(_ownerId);

        Assert.Equal(
            HttpStatusCode.OK,
            _response!.StatusCode);

        using var json = JsonDocument.Parse(
            await _response.Content.ReadAsStringAsync());

        var root = json.RootElement;

        Assert.NotEqual(
            Guid.Empty,
            root.GetProperty("clinicalRecordId").GetGuid());

        Assert.Equal(
            0,
            root.GetProperty("attentions").GetArrayLength());
    }

    [Then("el historial contiene cero atenciones")]
    public async Task ThenEmptyHistory()
    {
        using var json = JsonDocument.Parse(
            await _response!.Content.ReadAsStringAsync());

        Assert.Equal(
            0,
            json.RootElement
                .GetProperty("attentions")
                .GetArrayLength());
    }

    private async Task RegisterAsync(string name)
    {
        await SendAsync(
            HttpMethod.Post,
            "/api/v1/pets",
            new
            {
                name,
                species = "Dog",
                birthDate = "2022-05-10"
            },
            _ownerId,
            _role);

        if (_response!.StatusCode == HttpStatusCode.Created)
        {
            using var json = JsonDocument.Parse(
                await _response.Content.ReadAsStringAsync());

            _petId = json.RootElement
                .GetProperty("petId").GetGuid();
        }
    }

    private Task GetHistoryAsync(Guid userId)
        => SendAsync(
            HttpMethod.Get,
            $"/api/v1/pets/{_petId}/clinical-records",
            null,
            userId,
            "owner");

    private async Task SendAsync(
        HttpMethod method,
        string path,
        object? body,
        Guid userId,
        string? role)
    {
        using var request = new HttpRequestMessage(
            method, path);

        if (body is not null)
            request.Content = JsonContent.Create(body);

        if (userId != Guid.Empty &&
            !string.IsNullOrWhiteSpace(role))
        {
            request.Headers.Add(
                "X-Test-User-Id",
                userId.ToString());

            request.Headers.Add(
                "X-Test-Role",
                role);
        }

        _response?.Dispose();

        _response = await _client.SendAsync(request);
    }

    [AfterScenario("pet")]
    public async Task Cleanup()
    {
        try
        {
            if (_petId != Guid.Empty && _factory is not null)
            {
                using var scope = _factory.Services.CreateScope();

                var db = scope.ServiceProvider
                    .GetRequiredService<VetPaxDbContext>();

                var petId = new PetId(_petId);

                var record = await db.ClinicalRecords
                    .SingleOrDefaultAsync(r => r.PetId == petId);

                if (record is not null)
                    db.ClinicalRecords.Remove(record);

                var pet = await db.Pets
                    .SingleOrDefaultAsync(p => p.PetId == petId);

                if (pet is not null)
                    db.Pets.Remove(pet);

                await db.SaveChangesAsync();
            }
        }
        finally
        {
            _response?.Dispose();
            _client?.Dispose();
            _factory?.Dispose();
        }
    }
}
