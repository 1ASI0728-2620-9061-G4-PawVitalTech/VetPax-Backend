using Microsoft.Extensions.Options;

namespace VetPax.Api.Modules.MedicationTreatment.BackgroundServices;

public static class MedicationBackgroundServicesRegistration
{
    public static IServiceCollection AddMedicationBackgroundServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddOptions<MedicationWorkerOptions>()
            .Bind(configuration.GetSection(
                MedicationWorkerOptions.SectionName))
            .Validate(
                options => options.IsValid(),
                "MedicationTreatment worker configuration is invalid.")
            .ValidateOnStart();

        services.AddHostedService<
            MedicationTreatmentBackgroundService>();

        return services;
    }
}