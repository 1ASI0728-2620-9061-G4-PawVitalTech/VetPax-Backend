using Microsoft.Extensions.DependencyInjection;
using VetPax.Application.Modules.PetClinicalCare.Ports;
using VetPax.Application.Modules.PetClinicalCare.UseCases.LinkPetToClinic;
using VetPax.Infrastructure.Modules.PetClinicalCare.Adapters;

namespace VetPax.Infrastructure.Modules.PetClinicalCare;

public static class PetClinicLinksRegistration
{
    public static IServiceCollection AddPetClinicLinks(
        this IServiceCollection services)
    {
        services.AddScoped<
            IPetClinicLinkPort,
            PetClinicLinkAdapter>();

        services.AddScoped<
            IClinicDirectoryPort,
            PetClinicLinkAdapter>();

        services.AddScoped<
            IPetClinicPatientReadPort,
            PetClinicPatientsReadAdapter>();

        services.AddScoped<LinkPetToClinicHandler>();

        return services;
    }
}