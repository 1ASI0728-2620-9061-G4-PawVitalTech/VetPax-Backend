
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using System.Text.Json.Serialization;

using VetPax.Api;
using VetPax.Api.HostedServices;
using VetPax.Api.Modules.IdentityAccessManagement;
using VetPax.Api.Modules.MedicationTreatment.BackgroundServices;
using VetPax.Api.Modules.NutritionManagement.Adapters;
using VetPax.Api.Modules.NutritionManagement.BackgroundServices;
using VetPax.Api.Modules.AdherenceGamification.Adapters;
using VetPax.Api.Modules.AdherenceGamification.BackgroundServices;

using VetPax.Application.Common.Ports;
using VetPax.Application.Modules.PetClinicalCare.UseCases.GetClinicalHistory;
using VetPax.Application.Modules.PetClinicalCare.UseCases.GetPet;
using VetPax.Application.Modules.PetClinicalCare.UseCases.RegisterPet;
using VetPax.Application.Modules.NutritionManagement.Ports;
using VetPax.Application.Modules.AdherenceGamification.Ports;

using VetPax.Domain.Modules.PetClinicalCare.Repositories;

using VetPax.Infrastructure.Persistence;
using VetPax.Infrastructure.Messaging;
using VetPax.Infrastructure.Modules.IdentityAccessManagement;
using VetPax.Infrastructure.Modules.PetClinicalCare;
using VetPax.Infrastructure.Modules.PetClinicalCare.Repositories;
using VetPax.Infrastructure.Modules.ClinicManagement;
using VetPax.Infrastructure.Modules.AppointmentManagement;
using VetPax.Infrastructure.Modules.AppointmentManagement.Processing;
using VetPax.Infrastructure.Modules.MedicationTreatment;
using VetPax.Infrastructure.Modules.MedicationTreatment.Notifications;
using VetPax.Infrastructure.Modules.NutritionManagement;
using VetPax.Infrastructure.Modules.AdherenceGamification;
using VetPax.Infrastructure.Modules.AdherenceGamification.Adapters;

var builder = WebApplication.CreateBuilder(args);


//
// CORS - Angular Web / VetPax
//

var allowedOrigins = builder.Configuration
                         .GetSection("Cors:AllowedOrigins")
                         .Get<string[]>()
                     ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("VetPaxWeb", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});


// ============================================================
// 1. CONTROLLERS Y JSON
// ============================================================

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter()
        );
    });

// ============================================================
// 2. CONFIGURACIÓN DE KEYCLOAK PARA OPENAPI / SWAGGER
// ============================================================

// Los valores se obtienen de appsettings.json (local)
// o de las variables de entorno de Railway (producción).

var keycloakBaseUrl =
    builder.Configuration["Keycloak:BaseUrl"]
    ?.Trim().TrimEnd('/');

var keycloakRealm =
    builder.Configuration["Keycloak:Realm"]
    ?.Trim();

if (string.IsNullOrWhiteSpace(keycloakBaseUrl) ||
    string.IsNullOrWhiteSpace(keycloakRealm))
{
    throw new InvalidOperationException(
        "Keycloak BaseUrl or Realm is not configured."
    );
}

// Cliente público de Keycloak usado por Swagger.
// Por defecto usamos vetpax-dev, que ya existe en VetPax.
// Se puede cambiar mediante Swagger__OAuthClientId.

var swaggerOAuthClientId =
    builder.Configuration["Swagger:OAuthClientId"]
    ?? "vetpax-dev";

// ============================================================
// 3. OPENAPI + OAUTH2 AUTHORIZATION CODE CON PKCE
// ============================================================

builder.Services.AddOpenApi(options =>
{
    var securityScheme = new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.OAuth2,

        Flows = new OpenApiOAuthFlows
        {
            AuthorizationCode = new OpenApiOAuthFlow
            {
                AuthorizationUrl = new Uri(
                    $"{keycloakBaseUrl}/realms/{keycloakRealm}/protocol/openid-connect/auth"
                ),

                TokenUrl = new Uri(
                    $"{keycloakBaseUrl}/realms/{keycloakRealm}/protocol/openid-connect/token"
                ),

                Scopes = new Dictionary<string, string>
                {
                    ["openid"] = "OpenID",
                    ["profile"] = "Profile",
                    ["email"] = "Email"
                }
            }
        }
    };

    options.AddDocumentTransformer(
        (document, context, cancellationToken) =>
        {
            document.Components ??= new OpenApiComponents();

            document.Components.SecuritySchemes ??=
                new Dictionary<string, IOpenApiSecurityScheme>();

            document.Components.SecuritySchemes["oidc"] =
                securityScheme;

            var oidcReference =
                new OpenApiSecuritySchemeReference(
                    "oidc",
                    document
                );

            document.Security ??=
                new List<OpenApiSecurityRequirement>();

            document.Security.Add(
                new OpenApiSecurityRequirement
                {
                    [oidcReference] = new List<string>
                    {
                        "openid",
                        "profile",
                        "email"
                    }
                }
            );
            
            // Indicar a Swagger la URL pública HTTPS de Railway.
            // En desarrollo local se mantiene la URL automática.
            var publicApiUrl =
                builder.Configuration["Swagger:PublicBaseUrl"];

            if (!builder.Environment.IsDevelopment() &&
                Uri.TryCreate(
                    publicApiUrl,
                    UriKind.Absolute,
                    out var publicUri) &&
                publicUri.Scheme == Uri.UriSchemeHttps)
            {
                document.Servers = new List<OpenApiServer>
                {
                    new()
                    {
                        Url = publicUri.GetLeftPart(
                            UriPartial.Authority)
                    }
                };
            }

            return Task.CompletedTask;
            
        }
    );
});

// ============================================================
// 4. MYSQL / ENTITY FRAMEWORK CORE
// ============================================================

var connectionString =
    builder.Configuration.GetConnectionString("VetPaxDb")
    ?? throw new InvalidOperationException(
        "VetPax database connection is not configured."
    );

builder.Services.AddDbContext<VetPaxDbContext>(options =>
    options.UseMySql(
        connectionString,
        new MySqlServerVersion(new Version(8, 0, 0))
    )
);

// ============================================================
// 5. PET & CLINICAL CARE
// ============================================================

builder.Services.AddScoped<IPetRepository, PetRepository>();

builder.Services.AddScoped<
    IClinicalRecordRepository,
    ClinicalRecordRepository>();

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddScoped<RegisterPetHandler>();
builder.Services.AddScoped<GetPetHandler>();
builder.Services.AddScoped<GetClinicalHistoryHandler>();

builder.Services.AddVeterinaryClinicalAuthorization();
builder.Services.AddPetClinicalCare();
builder.Services.AddPetClinicLinks();

// ============================================================
// 6. CLINIC MANAGEMENT
// ============================================================

builder.Services.AddClinicManagementInfrastructure();

// ============================================================
// 7. APPOINTMENT MANAGEMENT
// ============================================================

builder.Services.AddAppointmentManagementInfrastructure();

builder.Services.AddScoped<AppointmentOutboxProcessor>();
builder.Services.AddScoped<AppointmentReminderProcessor>();

builder.Services.AddHttpClient<
    IAppointmentReminderSender,
    AppointmentReminderWebhookSender>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHostedService<
    AppointmentDeliveryHostedService>();

// ============================================================
// 8. MEDICATION TREATMENT
// ============================================================

builder.Services.AddMedicationTreatmentInfrastructure();

builder.Services.AddMedicationFirebase(
    builder.Configuration
);

builder.Services.AddMedicationBackgroundServices(
    builder.Configuration
);

// ============================================================
// 9. NUTRITION MANAGEMENT
// ============================================================

builder.Services.AddNutritionReminderWorker(
    builder.Configuration
);

builder.Services.AddScoped<
    INutritionIdentityPort,
    NutritionHttpIdentityAdapter>();

builder.Services.AddNutritionManagementInfrastructure();

// ============================================================
// 10. IDENTITY & ACCESS MANAGEMENT / KEYCLOAK
// ============================================================

builder.Services.AddIamInfrastructure(
    builder.Configuration,
    builder.Environment
);

builder.Services.AddIamApi();

builder.Services.AddHttpContextAccessor();

// ============================================================
// 11. ADHERENCE & GAMIFICATION
// ============================================================

builder.Services.AddScoped<
    IAdherenceCurrentOwner,
    HttpAdherenceCurrentOwner>();

builder.Services.AddAdherenceGamificationInfrastructure(
    builder.Configuration
);

builder.Services.AddHostedService<
    AdherenceRecognitionWorker>();

builder.Services.AddScoped<
    DevelopmentDomainEventPublisher>();

builder.Services.AddScoped<
    IDomainEventPublisher,
    AdherenceDomainEventPublisher>();

builder.Services.AddHostedService<
    AdherenceOutboxWorker>();

// ============================================================
// 12. BUILD APPLICATION
// ============================================================

var app = builder.Build();

app.UseStaticFiles();

// ============================================================
// 13. SWAGGER / OPENAPI
// ============================================================

// Development: siempre habilitado.
// Production: solamente si Swagger__Enabled=true.


var swaggerEnabled =
    app.Environment.IsDevelopment() ||
    app.Configuration.GetValue<bool>("Swagger:Enabled");

if (swaggerEnabled)
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/openapi/v1.json",
            "VetPax API v1"
        );

        options.OAuthClientId(
            builder.Configuration["Swagger:OAuthClientId"]
            ?? "vetpax-dev"
        );

        options.OAuthUsePkce();

        options.OAuthScopes(
            "openid",
            "profile",
            "email"
        );

        // Personalizar el botón Logout de Swagger.
        options.InjectJavascript("/swagger-logout.js");
    });
}


// ============================================================
// 14. ENTITY FRAMEWORK MIGRATIONS
// ============================================================

// En desarrollo se aplican automáticamente.
// En Railway Production deben ejecutarse por separado.

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();

    var dbContext = scope.ServiceProvider
        .GetRequiredService<VetPaxDbContext>();

    await dbContext.Database.MigrateAsync();
}

// ============================================================
// 15. HTTP PIPELINE
// ============================================================

// Railway termina HTTPS en su proxy.
// La conexión interna hacia Kestrel utiliza HTTP.
// Por eso no forzamos HTTPS dentro del contenedor;
// el dominio público de Railway debe utilizar HTTPS.

app.UseRouting();

// Permitir solicitudes desde Angular.
app.UseCors("VetPaxWeb");

// Rate limiting de los endpoints.
app.UseRateLimiter();

// Autenticación JWT con Keycloak.
app.UseAuthentication();
app.UseAuthorization();

// Endpoints de VetPax.
app.MapControllers();

app.Run();

public partial class Program { }
