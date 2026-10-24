
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using VetPax.Application.Modules.NutritionManagement.Ports;
using VetPax.Application.Modules.NutritionManagement.Services;
using VetPax.Application.Modules.NutritionManagement.UseCases;
using VetPax.Domain.Modules.NutritionManagement.Repositories;
using VetPax.Domain.Modules.PetClinicalCare.Repositories;
using VetPax.Infrastructure.Modules.NutritionManagement.Adapters;
using VetPax.Infrastructure.Modules.NutritionManagement.Repositories;
using VetPax.Infrastructure.Modules.PetClinicalCare.Repositories;

namespace VetPax.Infrastructure.Modules.NutritionManagement;

public static class DependencyInjection
{
    public static IServiceCollection AddNutritionManagementInfrastructure(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Shared clock
        services.TryAddSingleton<TimeProvider>(
            TimeProvider.System);

        // Existing PetClinicalCare repository
        services.TryAddScoped<
            IPetRepository,
            PetRepository>();

        // Nutrition repository
        services.AddScoped<
            IFeedingPlanRepository,
            FeedingPlanRepository>();

        // Persistence adapters
        services.AddScoped<
            INutritionUnitOfWork,
            NutritionUnitOfWorkAdapter>();

        services.AddScoped<
            INutritionOutboxPort,
            NutritionOutboxAdapter>();

        services.AddScoped<
            INutritionReminderPort,
            NutritionReminderAdapter>();

        services.AddScoped<
            INutritionReminderCandidatesPort,
            NutritionReminderCandidatesAdapter>();

        // Cross-context adapters
        services.AddScoped<
            INutritionPatientPort,
            NutritionPatientAdapter>();

        services.AddScoped<
            INutritionVeterinarianPort,
            NutritionVeterinarianAdapter>();

        // Application services
        services.AddScoped<NutritionAuthorizationService>();
        services.AddScoped<NutritionScheduleFactory>();
        services.AddSingleton<NutritionTimeService>();
        services.AddScoped<NutritionCommitService>();

        // Application handlers
        services.AddScoped<CreateFeedingPlanHandler>();
        services.AddScoped<UpdateFeedingPlanHandler>();
        services.AddScoped<DeactivateFeedingPlanHandler>();
        services.AddScoped<GetFeedingPlanHandler>();
        services.AddScoped<GetActiveFeedingPlanHandler>();
        services.AddScoped<ConfigureFeedingRemindersHandler>();
        services.AddScoped<GenerateFeedingRemindersHandler>();

        return services;
    }
}
