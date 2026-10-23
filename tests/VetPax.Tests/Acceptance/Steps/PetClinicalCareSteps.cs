
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

    [BeforeScenario]
    public void Setup()
    {
        _factory = new VetPaxApiFactory();
        _client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });
    }

    [Given("que soy un propietario nuevo")]
    public void GivenOwner()
    {
        _ownerId = Guid.NewGuid();
    }

    [Given("que ya registré una mascota")]
    public async Task GivenRegisteredPet()
    {
        GivenOwner();
        await RegisterAsync("Luna");
        Assert.Equal(HttpStatusCode.Created, _response!.StatusCode);
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
        Assert.Equal(expected, (int)_response!.StatusCode);
    }

    [Then("se crea un historial clínico vacío")]
    public async Task ThenRecordCreated()
    {
        await GetHistoryAsync(_ownerId);

        Assert.Equal(HttpStatusCode.OK, _response!.StatusCode);

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
            json.RootElement.GetProperty("attentions")
                .GetArrayLength());
    }

    private async Task RegisterAsync(string name)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post, "/api/v1/pets");

        request.Headers.Add(
            "X-Dev-Owner-Id", _ownerId.ToString());

        request.Content = JsonContent.Create(new
        {
            name,
            species = "Dog",
            birthDate = "2022-05-10"
        });

        _response?.Dispose();
        _response = await _client.SendAsync(request);

        if (_response.StatusCode == HttpStatusCode.Created)
        {
            using var json = JsonDocument.Parse(
                await _response.Content.ReadAsStringAsync());

            _petId = json.RootElement
                .GetProperty("petId").GetGuid();
        }
    }

    private async Task GetHistoryAsync(Guid ownerId)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/v1/pets/{_petId}/clinical-records");

        request.Headers.Add(
            "X-Dev-Owner-Id", ownerId.ToString());

        _response?.Dispose();
        _response = await _client.SendAsync(request);
    }

    [AfterScenario]
    public async Task Cleanup()
    {
        try
        {
            if (_petId != Guid.Empty && _factory is not null)
            {
                using var scope = _factory.Services.CreateScope();
                var db = scope.ServiceProvider
                    .GetRequiredService<VetPaxDbContext>();

                var id = new PetId(_petId);

                var record = await db.ClinicalRecords
                    .SingleOrDefaultAsync(r => r.PetId == id);

                if (record is not null)
                    db.ClinicalRecords.Remove(record);

                var pet = await db.Pets
                    .SingleOrDefaultAsync(p => p.PetId == id);

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
