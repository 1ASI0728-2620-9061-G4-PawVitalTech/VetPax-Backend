
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using VetPax.Application.Modules.MedicationTreatment.Ports;
using VetPax.Application.Modules.MedicationTreatment.Services;

using VetPax.Application.Modules.MedicationTreatment.UseCases.CreateMedicationPlan;
using VetPax.Application.Modules.MedicationTreatment.UseCases.UpdateMedicationPlan;
using VetPax.Application.Modules.MedicationTreatment.UseCases.GetMedicationPlans;
using VetPax.Application.Modules.MedicationTreatment.UseCases.GetMedicationPlan;
using VetPax.Application.Modules.MedicationTreatment.UseCases.ConfigureMedicationReminders;
using VetPax.Application.Modules.MedicationTreatment.UseCases.GetMedicationDoses;
using VetPax.Application.Modules.MedicationTreatment.UseCases.RegisterDoseAdministration;
using VetPax.Application.Modules.MedicationTreatment.UseCases.GenerateMedicationDoses;

using VetPax.Domain.Modules.MedicationTreatment.Repositories;
using VetPax.Domain.Modules.PetClinicalCare.Repositories;

using VetPax.Infrastructure.Modules.MedicationTreatment.Adapters;
using VetPax.Infrastructure.Modules.MedicationTreatment.Processing;
using VetPax.Infrastructure.Modules.MedicationTreatment.Repositories;

using VetPax.Infrastructure.Modules.PetClinicalCare.Repositories;

namespace VetPax.Infrastructure.Modules.MedicationTreatment;

public static class DependencyInjection
{
    public static IServiceCollection AddMedicationTreatmentInfrastructure(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // -----------------------------------------
        // Existing PetClinicalCare repositories
        // -----------------------------------------

        services.TryAddScoped<
            IPetRepository,
            PetRepository>();

        services.TryAddScoped<
            IClinicalRecordRepository,
            ClinicalRecordRepository>();

        // -----------------------------------------
        // MedicationTreatment repository
        // -----------------------------------------

        services.AddScoped<
            IMedicationPlanRepository,
            MedicationPlanRepository>();

        // -----------------------------------------
        // Persistence ports
        // -----------------------------------------

        services.AddScoped<
            IMedicationReminderPort,
            MedicationReminderAdapter>();

        services.AddScoped<
            IMedicationOutboxPort,
            MedicationOutboxAdapter>();

        // -----------------------------------------
        // Cross-bounded-context adapters
        // -----------------------------------------

        services.AddScoped<
            IMedicationPatientPort,
            MedicationPatientAdapter>();

        services.AddScoped<
            IMedicationVeterinarianPort,
            MedicationVeterinarianAdapter>();

        services.AddScoped<
            IMedicationClinicalContextPort,
            MedicationClinicalContextAdapter>();

        // -----------------------------------------
        // Application services
        // -----------------------------------------

        services.AddScoped<
            MedicationAuthorizationService>();

        services.AddScoped<
            MedicationCommitService>();

        services.AddSingleton<
            MedicationTimeService>();

        services.TryAddSingleton<TimeProvider>(
            TimeProvider.System);

        // -----------------------------------------
        // Use cases
        // -----------------------------------------

        services.AddScoped<
            CreateMedicationPlanHandler>();

        services.AddScoped<
            UpdateMedicationPlanHandler>();

        services.AddScoped<
            GetMedicationPlansHandler>();

        services.AddScoped<
            GetMedicationPlanHandler>();

        services.AddScoped<
            ConfigureMedicationRemindersHandler>();

        services.AddScoped<
            GetMedicationDosesHandler>();

        services.AddScoped<
            RegisterDoseAdministrationHandler>();

        services.AddScoped<
            GenerateMedicationDosesHandler>();

        // -----------------------------------------
        // Outbox processing
        // -----------------------------------------

        services.AddScoped<
            MedicationOutboxProcessor>();

        // MedicationReminderProcessor requires
        // IMedicationNotificationSender.
        //
        // It will be registered when the real FCM
        // adapter is implemented in TS06.
        //
        // Do not register a fake notification sender.

        return services;
    }
}
