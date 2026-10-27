
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using VetPax.Application.Modules.AdherenceGamification.Handlers;
using VetPax.Application.Modules.AdherenceGamification.Ports;
using VetPax.Application.Modules.AdherenceGamification.Services;
using VetPax.Application.Modules.ClinicManagement.Ports;
using VetPax.Domain.Modules.AdherenceGamification.Repositories;
using VetPax.Domain.Modules.AdherenceGamification.ValueObjects;
using VetPax.Infrastructure.Modules.AdherenceGamification.Adapters;
using VetPax.Infrastructure.Modules.AdherenceGamification.Notifications;
using VetPax.Infrastructure.Modules.AdherenceGamification.Repositories;
using VetPax.Infrastructure.Modules.AdherenceGamification.Processing;
using VetPax.Application.Modules.PetClinicalCare.UseCases.GetMyPets;
using VetPax.Application.Modules.ClinicManagement.UseCases.ListClinics;
using VetPax.Infrastructure.Modules.ClinicManagement.Adapters;

namespace VetPax.Infrastructure.Modules.AdherenceGamification;

public static class DependencyInjection
{
    public static IServiceCollection AddAdherenceGamificationInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.TryAddSingleton<TimeProvider>(
            TimeProvider.System);

        // No official scoring thresholds have been
        // established yet. Require explicit configuration.
        services.AddSingleton(_ =>
        {
            var section = configuration.GetSection(
                "AdherenceGamification:Rules");

            if (!section.Exists())
            {
                throw new InvalidOperationException(
                    "AdherenceGamification rules are not configured.");
            }

            return new AdherencePolicy(
                section.GetValue<int>("MinimumEvidence"),
                section.GetValue<decimal>("BronzeThreshold"),
                section.GetValue<decimal>("SilverThreshold"),
                section.GetValue<decimal>("GoldThreshold"),
                section.GetValue<bool>("AllowDemotion"));
        });

        services.AddScoped<
            IAdherenceRepository,
            AdherenceRepository>();

        services.AddScoped<
            IAdherenceEvidenceStore,
            AdherenceEvidenceStore>();

        services.AddScoped<
            IAdherenceOutbox,
            AdherenceOutboxAdapter>();

        services.AddScoped<
            IAdherenceUnitOfWork,
            AdherenceUnitOfWork>();

        services.AddScoped<
            IAdherenceScoreCalculator,
            EvidenceCountScoreCalculator>();

        services.AddScoped<IngestAdherenceEvidenceHandler>();
        services.AddScoped<GetMyAdherenceHandler>();
        services.AddScoped<AdherenceEvidenceConsumer>();
        services.AddScoped<GetMyPetsHandler>();

        services.AddScoped<
            IRecognitionPushGateway,
            RecognitionFirebaseGateway>();

        services.AddScoped<RecognitionNotificationProcessor>();
        services.AddScoped<AdherenceOutboxProcessor>();
        services.AddScoped<
            IClinicDirectoryPort,
            ClinicDirectoryAdapter>();

        services.AddScoped<ListClinicsHandler>();
        
        

        return services;
    }
}
