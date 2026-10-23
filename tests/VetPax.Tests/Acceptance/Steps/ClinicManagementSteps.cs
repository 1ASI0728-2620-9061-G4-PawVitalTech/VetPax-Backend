
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;
using VetPax.Domain.Modules.PetClinicalCare.ValueObjects;
using VetPax.Infrastructure.Persistence;
using VetPax.Tests.Acceptance.Support;
using Xunit;

namespace VetPax.Tests.Acceptance.Steps;

[Binding]
public sealed class ClinicManagementSteps
{
    private VetPaxApiFactory _factory = null!;
    private TestVeterinarianDirectory _directory = null!;
    private HttpClient _client = null!;
    private HttpResponseMessage? _response;

    private readonly List<Guid> _clinicIds = new();

    private Guid _adminId;
    private Guid _clinicId;
    private Guid _vetId;
    private Guid _ownerId;
    private Guid _petId;

    [BeforeScenario("clinic")]
    public void Setup()
    {
        _factory = new VetPaxApiFactory();

        _client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                AllowAutoRedirect = false
            });

        _directory = _factory.Services
            .GetRequiredService<TestVeterinarianDirectory>();
    }

    [Given("que soy administrador de clínica")]
    public void GivenAdmin()
    {
        _adminId = Guid.NewGuid();
    }

    [Given("que administro una clínica")]
    public async Task GivenClinic()
    {
        GivenAdmin();
        await CreateClinicAsync();

        Assert.Equal(
            HttpStatusCode.Created,
            _response!.StatusCode);
    }

    [Given("tengo un veterinario vinculado")]
    public async Task GivenVeterinarian()
    {
        _vetId = Guid.NewGuid();
        _directory.Allow(_vetId);

        await SendAsync(
            HttpMethod.Post,
            $"/api/v1/clinics/{_clinicId}/veterinarians",
            new { veterinarianId = _vetId },
            _adminId,
            "clinic_administrator");

        Assert.Equal(
            HttpStatusCode.NoContent,
            _response!.StatusCode);
    }

    [Given("hay una mascota vinculada a la clínica")]
    public async Task GivenPetLinked()
    {
        _ownerId = Guid.NewGuid();

        await SendAsync(
            HttpMethod.Post,
            "/api/v1/pets",
            new
            {
                name = "Luna",
                species = "Dog",
                birthDate = "2022-05-10"
            },
            _ownerId,
            "owner");

        Assert.Equal(
            HttpStatusCode.Created,
            _response!.StatusCode);

        using (var json = JsonDocument.Parse(
                   await _response.Content.ReadAsStringAsync()))
        {
            _petId = json.RootElement
                .GetProperty("petId").GetGuid();
        }

        await SendAsync(
            HttpMethod.Put,
            $"/api/v1/pets/{_petId}/clinics/{_clinicId}",
            null,
            _ownerId,
            "owner");

        Assert.Equal(
            HttpStatusCode.NoContent,
            _response!.StatusCode);
    }

    [When("creo una clínica con horarios válidos")]
    public Task WhenCreateClinic()
        => CreateClinicAsync();

    [When("actualizo el perfil con horarios válidos")]
    public Task WhenUpdateValid()
        => SendAsync(
            HttpMethod.Put,
            $"/api/v1/clinics/{_clinicId}",
            ClinicBody(false),
            _adminId,
            "clinic_administrator");

    [When("actualizo el perfil con horarios superpuestos")]
    public Task WhenUpdateInvalid()
        => SendAsync(
            HttpMethod.Put,
            $"/api/v1/clinics/{_clinicId}",
            ClinicBody(true),
            _adminId,
            "clinic_administrator");

    [When("un veterinario modifica el perfil")]
    public Task WhenVetUpdates()
        => SendAsync(
            HttpMethod.Put,
            $"/api/v1/clinics/{_clinicId}",
            ClinicBody(false),
            Guid.NewGuid(),
            "veterinarian");

    [When("intento vincular una cuenta no veterinaria")]
    public Task WhenUnverifiedVet()
        => SendAsync(
            HttpMethod.Post,
            $"/api/v1/clinics/{_clinicId}/veterinarians",
            new { veterinarianId = Guid.NewGuid() },
            _adminId,
            "clinic_administrator");

    [When("el veterinario consulta los pacientes")]
    public Task WhenGetPatients()
        => SendAsync(
            HttpMethod.Get,
            $"/api/v1/clinics/{_clinicId}/patients",
            null,
            _vetId,
            "veterinarian");

    [When("otro veterinario consulta los pacientes")]
    public Task WhenOtherVetGetsPatients()
        => SendAsync(
            HttpMethod.Get,
            $"/api/v1/clinics/{_clinicId}/patients",
            null,
            Guid.NewGuid(),
            "veterinarian");

    [Then(@"el código HTTP de clínica es (\d+)")]
    public void ThenStatus(int status)
    {
        Assert.Equal(status, (int)_response!.StatusCode);
    }

    [Then("la lista contiene la mascota vinculada")]
    public async Task ThenLinkedPetIsVisible()
    {
        using var json = JsonDocument.Parse(
            await _response!.Content.ReadAsStringAsync());

        var patients = json.RootElement
            .GetProperty("patients")
            .EnumerateArray();

        Assert.Contains(
            patients,
            p => p.GetProperty("petId").GetGuid() == _petId);
    }

    [Then("la lista de pacientes está vacía")]
    public async Task ThenEmptyPatients()
    {
        using var json = JsonDocument.Parse(
            await _response!.Content.ReadAsStringAsync());

        Assert.Equal(
            0,
            json.RootElement
                .GetProperty("patients")
                .GetArrayLength());
    }

    private object ClinicBody(bool overlap)
    {
        return new
        {
            name = "VetPax Clínica Central",
            address = "Av. Central 123",
            phone = "999888777",
            contactEmail = "clinica@vetpax.test",

            openingHours = overlap
                ? new[]
                {
                    new
                    {
                        dayOfWeek = "Monday",
                        openTime = "09:00:00",
                        closeTime = "13:00:00"
                    },
                    new
                    {
                        dayOfWeek = "Monday",
                        openTime = "12:00:00",
                        closeTime = "18:00:00"
                    }
                }
                : new[]
                {
                    new
                    {
                        dayOfWeek = "Monday",
                        openTime = "09:00:00",
                        closeTime = "18:00:00"
                    },
                    new
                    {
                        dayOfWeek = "Tuesday",
                        openTime = "09:00:00",
                        closeTime = "18:00:00"
                    }
                }
        };
    }

    private async Task CreateClinicAsync()
    {
        await SendAsync(
            HttpMethod.Post,
            "/api/v1/clinics",
            ClinicBody(false),
            _adminId,
            "clinic_administrator");

        if (_response!.StatusCode != HttpStatusCode.Created)
            return;

        using var json = JsonDocument.Parse(
            await _response.Content.ReadAsStringAsync());

        _clinicId = json.RootElement
            .GetProperty("clinicId").GetGuid();

        _clinicIds.Add(_clinicId);
    }

    private async Task SendAsync(
        HttpMethod method,
        string path,
        object? body,
        Guid userId,
        string role)
    {
        using var request = new HttpRequestMessage(
            method, path);

        if (body is not null)
            request.Content = JsonContent.Create(body);

        if (userId != Guid.Empty)
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

    [AfterScenario("clinic")]
    public async Task Cleanup()
    {
        try
        {
            if (_factory is null)
                return;

            using var scope = _factory.Services.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<VetPaxDbContext>();

            if (_petId != Guid.Empty)
            {
                var links = await db.PetClinicLinks
                    .Where(x => x.PetId == _petId)
                    .ToListAsync();

                db.PetClinicLinks.RemoveRange(links);

                var petId = new PetId(_petId);

                var record = await db.ClinicalRecords
                    .SingleOrDefaultAsync(
                        x => x.PetId == petId);

                if (record is not null)
                    db.ClinicalRecords.Remove(record);

                var pet = await db.Pets
                    .SingleOrDefaultAsync(
                        x => x.PetId == petId);

                if (pet is not null)
                    db.Pets.Remove(pet);
            }

            if (_clinicIds.Count > 0)
            {
                db.ClinicVeterinarians.RemoveRange(
                    await db.ClinicVeterinarians
                        .Where(x => _clinicIds.Contains(x.ClinicId))
                        .ToListAsync());

                db.ClinicOpeningHours.RemoveRange(
                    await db.ClinicOpeningHours
                        .Where(x => _clinicIds.Contains(x.ClinicId))
                        .ToListAsync());

                db.ClinicAdministrators.RemoveRange(
                    await db.ClinicAdministrators
                        .Where(x => _clinicIds.Contains(x.ClinicId))
                        .ToListAsync());

                db.Clinics.RemoveRange(
                    await db.Clinics
                        .Where(x => _clinicIds.Contains(x.ClinicId))
                        .ToListAsync());
            }

            await db.SaveChangesAsync();
        }
        finally
        {
            _response?.Dispose();
            _client?.Dispose();
            _factory?.Dispose();
        }
    }
}
