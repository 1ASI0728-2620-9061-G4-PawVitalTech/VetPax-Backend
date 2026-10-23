
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using VetPax.Application.Modules.AppointmentManagement.Ports;
using VetPax.Application.Modules.AppointmentManagement.Services;
using VetPax.Application.Modules.AppointmentManagement.UseCases.CancelAppointment;
using VetPax.Application.Modules.AppointmentManagement.UseCases.GetAppointment;
using VetPax.Application.Modules.AppointmentManagement.UseCases.GetVeterinarianAgenda;
using VetPax.Application.Modules.AppointmentManagement.UseCases.MarkAppointmentAttended;
using VetPax.Application.Modules.AppointmentManagement.UseCases.RescheduleAppointment;
using VetPax.Application.Modules.AppointmentManagement.UseCases.ScheduleAppointment;
using VetPax.Domain.Modules.AppointmentManagement.Repositories;
using VetPax.Infrastructure.Modules.AppointmentManagement.Adapters;
using VetPax.Infrastructure.Modules.AppointmentManagement.Repositories;

namespace VetPax.Infrastructure.Modules.AppointmentManagement;

public static class DependencyInjection
{
    public static IServiceCollection AddAppointmentManagementInfrastructure(
        this IServiceCollection services)
    {
        // TimeProvider compartido en todos los módulos.
        services.TryAddSingleton<TimeProvider>(
            TimeProvider.System);

        services.AddScoped<
            IAppointmentRepository,
            AppointmentRepository>();

        services.AddScoped<
            IAppointmentPatientPort,
            AppointmentPatientAdapter>();

        services.AddScoped<
            IAppointmentClinicPort,
            AppointmentClinicAdapter>();

        services.AddScoped<
            IAppointmentClinicalAttentionPort,
            AppointmentClinicalAttentionAdapter>();

        services.AddScoped<
            IAppointmentNotificationPort,
            AppointmentNotificationAdapter>();

        services.AddScoped<AppointmentAccessService>();

        services.AddScoped<ScheduleAppointmentHandler>();
        services.AddScoped<RescheduleAppointmentHandler>();
        services.AddScoped<CancelAppointmentHandler>();
        services.AddScoped<MarkAppointmentAttendedHandler>();
        services.AddScoped<GetVeterinarianAgendaHandler>();
        services.AddScoped<GetAppointmentHandler>();

        return services;
    }
}
