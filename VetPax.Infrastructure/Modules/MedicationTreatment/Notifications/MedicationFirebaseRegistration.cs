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
            {
                throw new InvalidOperationException(
                    "Firebase:ProjectId is not configured.");
            }

            var credentialsJson =
                configuration["Firebase:CredentialsJson"];

            var credential =
                string.IsNullOrWhiteSpace(credentialsJson)
                    ? GoogleCredential.GetApplicationDefault()
                    : GoogleCredential.FromJson(credentialsJson);

            var app = FirebaseApp.Create(
                new AppOptions
                {
                    Credential = credential,
                    ProjectId = projectId
                },
                $"VetPax-{Guid.NewGuid():N}");

            return FirebaseMessaging.GetMessaging(app);
        });

        return services;
    }
}