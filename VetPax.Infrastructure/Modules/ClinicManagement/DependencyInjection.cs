
using Microsoft.Extensions.DependencyInjection;
using VetPax.Application.Modules.ClinicManagement.Ports;
using VetPax.Application.Modules.ClinicManagement.Services;
using VetPax.Application.Modules.ClinicManagement.UseCases.AssignVeterinarian;
using VetPax.Application.Modules.ClinicManagement.UseCases.GetClinicPatients;
using VetPax.Application.Modules.ClinicManagement.UseCases.GetClinicProfile;
using VetPax.Application.Modules.ClinicManagement.UseCases.ListClinics;
using VetPax.Application.Modules.ClinicManagement.UseCases.ProvisionClinic;
using VetPax.Application.Modules.ClinicManagement.UseCases.UpdateClinicProfile;
using VetPax.Domain.Modules.ClinicManagement.Repositories;
using VetPax.Domain.Modules.ClinicManagement.Services;
using VetPax.Infrastructure.Modules.ClinicManagement.Adapters;
using VetPax.Infrastructure.Modules.ClinicManagement.Repositories;

namespace VetPax.Infrastructure.Modules.ClinicManagement;

public static class DependencyInjection
{
    public static IServiceCollection AddClinicManagementInfrastructure(
        this IServiceCollection services)
    {
        services.AddScoped<
            IClinicRepository,
            ClinicRepository>();

        services.AddScoped<
            IClinicMembershipPort,
            ClinicMembershipAdapter>();

        services.AddScoped<
            IClinicPatientsProvider,
            ClinicPatientsAdapter>();

        services.AddScoped<ClinicRequesterResolver>();
        services.AddScoped<ClinicAccessPolicy>();

        services.AddScoped<GetClinicProfileHandler>();
        services.AddScoped<UpdateClinicProfileHandler>();
        services.AddScoped<GetClinicPatientsHandler>();

        services.AddHttpClient<
            IVeterinarianIdentityDirectoryPort,
            KeycloakVeterinarianIdentityDirectoryAdapter>();

        services.AddScoped<
            IClinicVeterinarianWriterPort,
            ClinicVeterinarianWriterAdapter>();

        services.AddScoped<ProvisionClinicHandler>();
        services.AddScoped<AssignVeterinarianHandler>();
        services.AddScoped<IClinicDirectoryPort,
            ClinicDirectoryAdapter>();

        services.AddScoped<ListClinicsHandler>();
        services.AddScoped<ListClinicVeterinariansHandler>();

        return services;
    }
}
