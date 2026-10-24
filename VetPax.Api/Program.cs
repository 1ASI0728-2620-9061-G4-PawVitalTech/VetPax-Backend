
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using VetPax.Api;
using VetPax.Application.Common.Ports;
using VetPax.Application.Modules.PetClinicalCare.UseCases.GetClinicalHistory;
using VetPax.Application.Modules.PetClinicalCare.UseCases.GetPet;
using VetPax.Application.Modules.PetClinicalCare.UseCases.RegisterPet;
using VetPax.Domain.Modules.PetClinicalCare.Repositories;
using VetPax.Infrastructure.Modules.IdentityAccessManagement;
using VetPax.Infrastructure.Modules.PetClinicalCare.Repositories;
using VetPax.Infrastructure.Persistence;
using VetPax.Infrastructure.Modules.ClinicManagement;
using VetPax.Infrastructure.Modules.PetClinicalCare;
using VetPax.Infrastructure.Modules.AppointmentManagement;
using VetPax.Api.HostedServices;
using VetPax.Infrastructure.Modules.AppointmentManagement.Processing;
using VetPax.Infrastructure.Modules.MedicationTreatment;
using VetPax.Api.Modules.MedicationTreatment.BackgroundServices;
using VetPax.Infrastructure.Modules.MedicationTreatment.Notifications;

var builder = WebApplication.CreateBuilder(args);

// Controllers and JSON configuration
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter()
        );
    });

builder.Services.AddOpenApi();

// MySQL connection
var connectionString = builder.Configuration
    .GetConnectionString("VetPaxDb")
    ?? throw new InvalidOperationException(
        "VetPax database connection is not configured."
    );

builder.Services.AddDbContext<VetPaxDbContext>(options =>
    options.UseMySql(
        connectionString,
        new MySqlServerVersion(new Version(8, 0, 0))
    )
);

// PetClinicalCare dependency injection
builder.Services.AddScoped<IPetRepository, PetRepository>();

builder.Services.AddScoped<
    IClinicalRecordRepository,
    ClinicalRecordRepository>();

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<RegisterPetHandler>();
builder.Services.AddScoped<GetPetHandler>();
builder.Services.AddScoped<GetClinicalHistoryHandler>();
builder.Services.AddMedicationTreatmentInfrastructure();

builder.Services.AddVeterinaryClinicalAuthorization();
builder.Services.AddPetClinicalCare();
builder.Services.AddClinicManagementInfrastructure();
builder.Services.AddPetClinicLinks();
builder.Services.AddAppointmentManagementInfrastructure();

builder.Services.AddMedicationFirebase(builder.Configuration);

builder.Services.AddScoped<AppointmentOutboxProcessor>();

builder.Services.AddScoped<AppointmentReminderProcessor>();

builder.Services.AddMedicationBackgroundServices(
    builder.Configuration);

builder.Services.AddHttpClient<
    IAppointmentReminderSender,
    AppointmentReminderWebhookSender>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHostedService<
    AppointmentDeliveryHostedService>();


// Keycloak and JWT authentication
builder.Services.AddIamInfrastructure(
    builder.Configuration,
    builder.Environment
);

var app = builder.Build();

// Development configuration
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/openapi/v1.json",
            "VetPax API v1"
        );
    });

    using var scope = app.Services.CreateScope();

    var dbContext = scope.ServiceProvider
        .GetRequiredService<VetPaxDbContext>();

    await dbContext.Database.MigrateAsync();
}

// HTTP pipeline
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
