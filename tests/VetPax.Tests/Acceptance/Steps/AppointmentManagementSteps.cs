
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;
using VetPax.Domain.Modules.PetClinicalCare.ValueObjects;
using VetPax.Infrastructure.Modules.AppointmentManagement.Persistence;
using VetPax.Infrastructure.Persistence;
using VetPax.Tests.Acceptance.Support;
using Xunit;

namespace VetPax.Tests.Acceptance.Steps;

[Binding]
public sealed class AppointmentManagementSteps
{
    private VetPaxApiFactory _factory = null!;
    private TestVeterinarianDirectory _directory = null!;
    private HttpClient _client = null!;
    private HttpResponseMessage? _response;

    private Guid _adminId;
    private Guid _ownerId;
    private Guid _veterinarianId;
    private Guid _clinicId;
    private Guid _petId;
    private Guid _appointmentId;
    private Guid _clinicalAttentionId;

    private DateOnly _appointmentDate;

    [BeforeScenario("appointment")]
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

        // Siempre una fecha futura, en lunes.
        _appointmentDate = DateOnly.FromDateTime(
            DateTime.UtcNow.AddDays(8));

        while (_appointmentDate.DayOfWeek != DayOfWeek.Monday)
        {
            _appointmentDate = _appointmentDate.AddDays(1);
        }
    }

    // -------------------------------------------------
    // GIVEN: preparar clínica, veterinario y mascota
    // -------------------------------------------------

    [Given("que existe una clínica preparada para gestionar citas")]
    public async Task GivenClinicReady()
    {
        _adminId = Guid.NewGuid();
        _ownerId = Guid.NewGuid();
        _veterinarianId = Guid.NewGuid();

        _directory.Allow(_veterinarianId);

        // 1. Crear clínica con horario los lunes.
        await SendAsync(
            HttpMethod.Post,
            "/api/v1/clinics",
            new
            {
                name = "VetPax Clínica de Citas",
                address = "Av. Central 123",
                phone = "999888777",
                contactEmail = "citas@vetpax.test",
                openingHours = new[]
                {
                    new
                    {
                        dayOfWeek = "Monday",
                        openTime = "09:00:00",
                        closeTime = "18:00:00"
                    }
                }
            },
            _adminId,
            "clinic_administrator");

        await AssertStatusAsync(HttpStatusCode.Created);

        using (var json = await ReadResponseAsync())
        {
            _clinicId = json.RootElement
                .GetProperty("clinicId")
                .GetGuid();
        }

        // 2. Vincular veterinario autorizado.
        await SendAsync(
            HttpMethod.Post,
            $"/api/v1/clinics/{_clinicId}/veterinarians",
            new
            {
                veterinarianId = _veterinarianId
            },
            _adminId,
            "clinic_administrator");

        await AssertStatusAsync(HttpStatusCode.NoContent);

        // 3. Registrar mascota.
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

        await AssertStatusAsync(HttpStatusCode.Created);

        using (var json = await ReadResponseAsync())
        {
            _petId = json.RootElement
                .GetProperty("petId")
                .GetGuid();
        }

        // 4. Vincular mascota con clínica.
        await SendAsync(
            HttpMethod.Put,
            $"/api/v1/pets/{_petId}/clinics/{_clinicId}",
            null,
            _ownerId,
            "owner");

        await AssertStatusAsync(HttpStatusCode.NoContent);
    }

    [Given("que existe una cita pendiente a las {word}")]
    public async Task GivenPendingAppointment(string time)
    {
        await ScheduleAsync(
            ParseTime(time),
            _ownerId,
            "owner");

        await AssertStatusAsync(HttpStatusCode.Created);
    }

    [Given("existe otra cita pendiente a las {word}")]
    public async Task GivenAnotherAppointment(string time)
    {
        await GivenPendingAppointment(time);
    }

    // -------------------------------------------------
    // WHEN: US04
    // -------------------------------------------------

    [When("el propietario agenda una cita a las {word}")]
    public Task WhenOwnerSchedules(string time)
    {
        return ScheduleAsync(
            ParseTime(time),
            _ownerId,
            "owner");
    }

    [When("otro propietario intenta agendar una cita")]
    public Task WhenOtherOwnerSchedules()
    {
        return ScheduleAsync(
            new TimeOnly(9, 0),
            Guid.NewGuid(),
            "owner");
    }

    [When("un veterinario intenta agendar una cita")]
    public Task WhenVeterinarianSchedules()
    {
        return ScheduleAsync(
            new TimeOnly(9, 0),
            _veterinarianId,
            "veterinarian");
    }

    // -------------------------------------------------
    // WHEN: US05
    // -------------------------------------------------

    [When("el propietario reprograma la cita a las {word}")]
    public Task WhenOwnerReschedules(string time)
    {
        return SendAsync(
            HttpMethod.Patch,
            $"/api/v1/appointments/{_appointmentId}",
            new
            {
                operation = "reschedule",
                date = _appointmentDate,
                startTime = ParseTime(time)
            },
            _ownerId,
            "owner");
    }

    [Given("el propietario cancela la cita")]
    [When("el propietario cancela la cita")]
    public Task WhenOwnerCancels()
    {
        return SendAsync(
            HttpMethod.Patch,
            $"/api/v1/appointments/{_appointmentId}",
            new
            {
                operation = "cancel"
            },
            _ownerId,
            "owner");
    }

    // -------------------------------------------------
    // WHEN: consulta individual y agenda
    // -------------------------------------------------

    [When("otro propietario consulta la cita")]
    public Task WhenOtherOwnerGetsAppointment()
    {
        return SendAsync(
            HttpMethod.Get,
            $"/api/v1/appointments/{_appointmentId}",
            null,
            Guid.NewGuid(),
            "owner");
    }

    [When("el veterinario consulta su agenda")]
    public Task WhenVeterinarianGetsAgenda()
    {
        return GetAgendaAsync(_veterinarianId);
    }

    [When("un veterinario ajeno consulta la agenda")]
    public Task WhenOtherVeterinarianGetsAgenda()
    {
        return GetAgendaAsync(Guid.NewGuid());
    }

    // -------------------------------------------------
    // WHEN: US06
    // -------------------------------------------------

    [When("el propietario intenta marcar la cita como atendida")]
    public Task WhenOwnerMarksAttended()
    {
        return MarkAttendedAsync(
            _ownerId,
            "owner",
            Guid.NewGuid());
    }

    [When("el veterinario intenta marcar la cita sin atención vinculada")]
    public Task WhenVeterinarianMarksWithoutAttention()
    {
        return MarkAttendedAsync(
            _veterinarianId,
            "veterinarian",
            Guid.NewGuid());
    }

    
    [Given("existe una atención clínica vinculada mediante US03")]
    public async Task GivenLinkedClinicalAttention()
    {
        await SendAsync(
            HttpMethod.Post,
            $"/api/v1/pets/{_petId}/clinical-records" +
            $"?clinicId={_clinicId}" +
            $"&appointmentId={_appointmentId}",
            new
            {
                attentionDate = DateTime.UtcNow,
                reason = "Control veterinario general",
                diagnosis = "Paciente estable",
                treatmentSummary = "Seguimiento clínico",
                observations = "Sin incidencias",
                indicators = new[]
                {
                    new
                    {
                        name = "Weight",
                        value = 8.5m,
                        unit = "kg"
                    }
                }
            },
            _veterinarianId,
            "veterinarian");

        await AssertStatusAsync(HttpStatusCode.Created);

        using var json = await ReadResponseAsync();

        _clinicalAttentionId = json.RootElement
            .GetProperty("attentionId")
            .GetGuid();

        // No se inserta ningún vínculo manual.
        // Debe haberlo creado el flujo productivo de US03.
    }

    [When("el veterinario intenta registrar una atención para una cita inexistente")]
    public Task WhenRegisterAttentionForMissingAppointment()
    {
        return SendAsync(
            HttpMethod.Post,
            $"/api/v1/pets/{_petId}/clinical-records" +
            $"?clinicId={_clinicId}" +
            $"&appointmentId={Guid.NewGuid()}",
            new
            {
                attentionDate = DateTime.UtcNow,
                reason = "Control veterinario",
                diagnosis = "Paciente estable",
                treatmentSummary = "Seguimiento",
                observations = "Sin incidencias",
                indicators = Array.Empty<object>()
            },
            _veterinarianId,
            "veterinarian");
    }


    [When("el veterinario marca la cita como atendida")]
    public Task WhenVeterinarianMarksAttended()
    {
        return MarkAttendedAsync(
            _veterinarianId,
            "veterinarian",
            _clinicalAttentionId);
    }

    // -------------------------------------------------
    // THEN
    // -------------------------------------------------

    [Then("el código HTTP de citas es {int}")]
    public void ThenAppointmentStatus(int status)
    {
        Assert.NotNull(_response);

        Assert.Equal(
            status,
            (int)_response.StatusCode);
    }

    [Then("la respuesta de cita tiene estado {word}")]
    public async Task ThenAppointmentHasState(string expected)
    {
        using var json = await ReadResponseAsync();

        Assert.Equal(
            expected,
            json.RootElement
                .GetProperty("status")
                .GetString());
    }

    [Then("la respuesta de cita inicia a las {word}")]
    public async Task ThenAppointmentStartsAt(string expected)
    {
        using var json = await ReadResponseAsync();

        var actual = TimeOnly.Parse(
            json.RootElement
                .GetProperty("startTime")
                .GetString()!,
            CultureInfo.InvariantCulture);

        Assert.Equal(
            ParseTime(expected),
            actual);
    }

    [Then("la agenda contiene dos citas ordenadas")]
    public async Task ThenAgendaIsSorted()
    {
        using var json = await ReadResponseAsync();

        var appointments = json.RootElement
            .GetProperty("appointments")
            .EnumerateArray()
            .ToArray();

        Assert.Equal(2, appointments.Length);

        var firstTime = TimeOnly.Parse(
            appointments[0]
                .GetProperty("startTime")
                .GetString()!,
            CultureInfo.InvariantCulture);

        var secondTime = TimeOnly.Parse(
            appointments[1]
                .GetProperty("startTime")
                .GetString()!,
            CultureInfo.InvariantCulture);

        Assert.Equal(
            new TimeOnly(9, 0),
            firstTime);

        Assert.Equal(
            new TimeOnly(10, 0),
            secondTime);

        Assert.All(
            appointments,
            a => Assert.Equal(
                _veterinarianId,
                a.GetProperty("veterinarianId").GetGuid()));
    }

    // -------------------------------------------------
    // Helpers HTTP
    // -------------------------------------------------

    private async Task ScheduleAsync(
        TimeOnly startTime,
        Guid userId,
        string role)
    {
        await SendAsync(
            HttpMethod.Post,
            "/api/v1/appointments",
            new
            {
                petId = _petId,
                clinicId = _clinicId,
                veterinarianId = _veterinarianId,
                date = _appointmentDate,
                startTime,
                reason = "Control veterinario general"
            },
            userId,
            role);

        if (_response!.StatusCode == HttpStatusCode.Created)
        {
            using var json = await ReadResponseAsync();

            _appointmentId = json.RootElement
                .GetProperty("appointmentId")
                .GetGuid();
        }
    }

    private Task GetAgendaAsync(Guid veterinarianId)
    {
        var date = _appointmentDate.ToString(
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture);

        return SendAsync(
            HttpMethod.Get,
            "/api/v1/appointments/agenda" +
            $"?clinicId={_clinicId}" +
            $"&from={date}&to={date}",
            null,
            veterinarianId,
            "veterinarian");
    }

    private Task MarkAttendedAsync(
        Guid userId,
        string role,
        Guid attentionId)
    {
        return SendAsync(
            HttpMethod.Patch,
            $"/api/v1/appointments/{_appointmentId}",
            new
            {
                operation = "mark_attended",
                clinicalAttentionId = attentionId
            },
            userId,
            role);
    }

    private static TimeOnly ParseTime(string time)
    {
        return TimeOnly.ParseExact(
            time,
            "HH:mm",
            CultureInfo.InvariantCulture);
    }

    private async Task SendAsync(
        HttpMethod method,
        string path,
        object? body,
        Guid userId,
        string role)
    {
        using var request = new HttpRequestMessage(
            method,
            path);

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

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

    private async Task<JsonDocument> ReadResponseAsync()
    {
        Assert.NotNull(_response);

        return JsonDocument.Parse(
            await _response.Content.ReadAsStringAsync());
    }

    private async Task AssertStatusAsync(
        HttpStatusCode expected)
    {
        Assert.NotNull(_response);

        var body = await _response.Content.ReadAsStringAsync();

        Assert.True(
            _response.StatusCode == expected,
            $"Expected {(int)expected}, " +
            $"actual {(int)_response.StatusCode}. " +
            $"Response: {body}");
    }

    // -------------------------------------------------
    // CLEANUP: eliminar datos creados por el escenario
    // -------------------------------------------------

    [AfterScenario("appointment")]
    public async Task Cleanup()
    {
        try
        {
            if (_factory is null)
                return;

            using var scope = _factory.Services.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<VetPaxDbContext>();

            // 1. Eliminar registros de AppointmentManagement.
            if (_clinicId != Guid.Empty)
            {
                var appointments = await db.Appointments
                    .Where(x => x.ClinicId == _clinicId)
                    .ToListAsync();

                // CORRECCIÓN:
                // No usar appointmentIds.Contains(...)
                // dentro de consultas de EF Core.
                //
                // En la combinación actual de .NET 10 y EF Core
                // genera una evaluación de ReadOnlySpan<Guid>
                // que puede fallar durante TearDown.
                //
                // Consultamos por GUID individual.

                foreach (var appointment in appointments)
                {
                    var appointmentId = appointment.AppointmentId;

                    // Asociaciones con atenciones clínicas.
                    var attentionLinks =
                        await db.AppointmentAttentionLinks
                            .Where(x =>
                                x.AppointmentId == appointmentId)
                            .ToListAsync();

                    db.AppointmentAttentionLinks
                        .RemoveRange(attentionLinks);

                    // Recordatorios de esta cita.
                    var reminders =
                        await db.AppointmentReminders
                            .Where(x =>
                                x.AppointmentId == appointmentId)
                            .ToListAsync();

                    db.AppointmentReminders
                        .RemoveRange(reminders);

                    // Eventos de Outbox.
                    var outbox =
                        await db.AppointmentOutbox
                            .Where(x =>
                                x.AppointmentId == appointmentId)
                            .ToListAsync();

                    db.AppointmentOutbox
                        .RemoveRange(outbox);
                }

                // Eliminar citas después de sus dependencias.
                db.Appointments.RemoveRange(appointments);
            }

            // 2. Eliminar registros de la mascota.
            if (_petId != Guid.Empty)
            {
                var petLinks = await db.PetClinicLinks
                    .Where(x => x.PetId == _petId)
                    .ToListAsync();

                db.PetClinicLinks.RemoveRange(petLinks);

                var petId = new PetId(_petId);

                var record = await db.ClinicalRecords
                    .Include(x => x.Attentions)
                    .SingleOrDefaultAsync(
                        x => x.PetId == petId);

                if (record is not null)
                {
                    db.ClinicalRecords.Remove(record);
                }

                var pet = await db.Pets
                    .SingleOrDefaultAsync(
                        x => x.PetId == petId);

                if (pet is not null)
                {
                    db.Pets.Remove(pet);
                }
            }

            // 3. Eliminar relaciones y clínica.
            if (_clinicId != Guid.Empty)
            {
                var veterinarians =
                    await db.ClinicVeterinarians
                        .Where(x => x.ClinicId == _clinicId)
                        .ToListAsync();

                db.ClinicVeterinarians
                    .RemoveRange(veterinarians);

                var openingHours =
                    await db.ClinicOpeningHours
                        .Where(x => x.ClinicId == _clinicId)
                        .ToListAsync();

                db.ClinicOpeningHours
                    .RemoveRange(openingHours);

                var administrators =
                    await db.ClinicAdministrators
                        .Where(x => x.ClinicId == _clinicId)
                        .ToListAsync();

                db.ClinicAdministrators
                    .RemoveRange(administrators);

                var clinics = await db.Clinics
                    .Where(x => x.ClinicId == _clinicId)
                    .ToListAsync();

                db.Clinics.RemoveRange(clinics);
            }

            // Confirmar todas las eliminaciones.
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
