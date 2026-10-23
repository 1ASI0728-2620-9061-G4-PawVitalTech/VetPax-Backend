
using VetPax.Application.Common.Ports;
using VetPax.Application.Modules.PetClinicalCare.Ports;
using VetPax.Application.Modules.PetClinicalCare.UseCases.GetPet;
using VetPax.Application.Modules.PetClinicalCare.UseCases.RegisterPet;
using VetPax.Application.Modules.PetClinicalCare.UseCases.GetClinicalHistory;
using VetPax.Application.Modules.PetClinicalCare.UseCases.RegisterClinicalAttention;
using VetPax.Application.Modules.PetClinicalCare.UseCases.GetClinicalEvolution;
using VetPax.Domain.Modules.PetClinicalCare.Repositories;
using VetPax.Domain.Modules.PetClinicalCare.Services;
using VetPax.Infrastructure.Messaging;
using VetPax.Infrastructure.Persistence;
using VetPax.Infrastructure.Modules.PetClinicalCare.Repositories;
using VetPax.Infrastructure.Modules.PetClinicalCare.Adapters;

namespace VetPax.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddPetClinicalCare(
        this IServiceCollection services)
    {
        // Repositories
        services.AddScoped<IPetRepository, PetRepository>();
        services.AddScoped<
            IClinicalRecordRepository,
            ClinicalRecordRepository>();

        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Domain Services
        services.AddScoped<ClinicalAccessPolicy>();
        services.AddScoped<ClinicalEvolutionService>();

        // Application Handlers
        services.AddScoped<RegisterPetHandler>();
        services.AddScoped<GetPetHandler>();
        services.AddScoped<GetClinicalHistoryHandler>();
        services.AddScoped<RegisterClinicalAttentionHandler>();
        services.AddScoped<GetClinicalEvolutionHandler>();

        // Infrastructure Adapters
        services.AddScoped<
            IClinicalAuthorizationPort,
            VerifiedClinicalAuthorizationAdapter>();

        services.AddScoped<
            IDomainEventPublisher,
            DevelopmentDomainEventPublisher>();

        services.AddScoped<
            IClinicalUpdateNotifier,
            DevelopmentClinicalUpdateNotifier>();

        return services;
    }
}
