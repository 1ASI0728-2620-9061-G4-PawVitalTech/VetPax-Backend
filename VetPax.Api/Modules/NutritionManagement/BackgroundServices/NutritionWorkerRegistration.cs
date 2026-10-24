using Microsoft.Extensions.Options;
using VetPax.Application.Modules.NutritionManagement.Ports;
using VetPax.Infrastructure.Modules.NutritionManagement.Notifications;

namespace VetPax.Api.Modules.NutritionManagement.BackgroundServices;

public static class NutritionWorkerRegistration
{
    public static IServiceCollection AddNutritionReminderWorker(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<NutritionWorkerOptions>()
            .Bind(configuration.GetSection(
                NutritionWorkerOptions.SectionName))
            .Validate(
                options => options.IsValid(),
                "Invalid NutritionManagement worker options.")
            .ValidateOnStart();

        services.AddScoped<
            INutritionPushGateway,
            NutritionFirebasePushGateway>();

        services.AddScoped<NutritionReminderProcessor>();

        services.AddHostedService<NutritionBackgroundService>();

        return services;
    }
}