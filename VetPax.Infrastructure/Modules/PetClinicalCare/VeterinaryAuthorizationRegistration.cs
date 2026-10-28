using Microsoft.Extensions.DependencyInjection;
using VetPax.Application.Modules.PetClinicalCare.Ports;
using VetPax.Application.Modules.PetClinicalCare.UseCases.AuthorizeVeterinaryAccess;
using VetPax.Domain.Modules.PetClinicalCare.Services;
using VetPax.Infrastructure.Modules.PetClinicalCare.Adapters;

namespace VetPax.Infrastructure.Modules.PetClinicalCare;

public static class VeterinaryAuthorizationRegistration
{
    public static IServiceCollection AddVeterinaryClinicalAuthorization(
        this IServiceCollection services)
    {
        services.AddScoped<
            IClinicalRelationshipPort,
            ClinicalRelationshipAdapter>();

        services.AddScoped<VeterinaryClinicalAccessPolicy>();

        services.AddScoped<AuthorizeVeterinaryAccessHandler>();

        return services;
    }
}