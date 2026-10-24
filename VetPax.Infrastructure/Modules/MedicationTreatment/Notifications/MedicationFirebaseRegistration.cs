using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace VetPax.Infrastructure.Modules.MedicationTreatment.Notifications;

public static class MedicationFirebaseRegistration
{
    public static IServiceCollection AddMedicationFirebase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddScoped<MedicationDeviceTokenStore>();
        services.AddScoped<
            IMedicationPushGateway,
            MedicationFirebasePushGateway>();
        services.AddSingleton<FirebaseMessaging>(_ =>
        {
            var projectId = configuration["Firebase:ProjectId"];

            if (string.IsNullOrWhiteSpace(projectId))
                throw new InvalidOperationException(
                    "Firebase:ProjectId is not configured.");

            var app = FirebaseApp.Create(
                new AppOptions
                {
                    Credential = GoogleCredential.GetApplicationDefault(),
                    ProjectId = projectId
                },
                $"VetPaxMedication-{Guid.NewGuid():N}");

            return FirebaseMessaging.GetMessaging(app);
        });

        services.AddSingleton(_ =>
        {
            var projectId = configuration["Firebase:ProjectId"];

            if (string.IsNullOrWhiteSpace(projectId))
            {
                throw new InvalidOperationException(
                    "Firebase:ProjectId is not configured.");
            }

            // Application Default Credentials.
            // No almacenar credenciales dentro del proyecto.
            var app = FirebaseApp.Create(
                new AppOptions
                {
                    Credential =
                        GoogleCredential.GetApplicationDefault(),

                    ProjectId = projectId
                },
                $"VetPaxMedication-{Guid.NewGuid():N}");

            return FirebaseMessaging.GetMessaging(app);
        });

        return services;
    }
}