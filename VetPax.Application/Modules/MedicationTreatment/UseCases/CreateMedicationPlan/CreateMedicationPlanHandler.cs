
using VetPax.Application.Modules.MedicationTreatment.Models;
using VetPax.Application.Modules.MedicationTreatment.Ports;
using VetPax.Application.Modules.MedicationTreatment.Services;
using VetPax.Domain.Modules.MedicationTreatment.Aggregates;
using VetPax.Domain.Modules.MedicationTreatment.ValueObjects;

namespace VetPax.Application.Modules.MedicationTreatment.UseCases.CreateMedicationPlan;

public sealed class CreateMedicationPlanHandler
{
    private readonly MedicationAuthorizationService _authorization;
    private readonly IMedicationClinicalContextPort _clinicalContext;
    private readonly MedicationTimeService _time;
    private readonly MedicationCommitService _commit;
    private readonly TimeProvider _clock;

    public CreateMedicationPlanHandler(
        MedicationAuthorizationService authorization,
        IMedicationClinicalContextPort clinicalContext,
        MedicationTimeService time,
        MedicationCommitService commit,
        TimeProvider clock)
    {
        _authorization = authorization;
        _clinicalContext = clinicalContext;
        _time = time;
        _commit = commit;
        _clock = clock;
    }

    public async Task<MedicationPlanData> HandleAsync(
        CreateMedicationPlanCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var (veterinarianId, ownerId) =
            await _authorization.RequirePrescriberAsync(
                command.PetId,
                command.ClinicId,
                cancellationToken);

        _time.Resolve(command.TimeZoneId);

        if (command.ClinicalAttentionId.HasValue)
        {
            var valid = await _clinicalContext.MatchesAsync(
                command.ClinicalAttentionId.Value,
                command.PetId,
                command.ClinicId,
                veterinarianId,
                cancellationToken);

            if (!valid)
            {
                throw new UnauthorizedAccessException(
                    "Clinical attention does not match this prescription.");
            }
        }

        var schedules = MedicationScheduleFactory.Create(
            command.Schedules);

        var now = _clock.GetUtcNow().UtcDateTime;

        var plan = MedicationPlan.Prescribe(
            new MedicationOwnerId(ownerId),
            new MedicationPetId(command.PetId),
            new MedicationClinicId(command.ClinicId),
            new MedicationVeterinarianId(veterinarianId),
            command.MedicationName,
            command.Route,
            command.Instructions,
            command.StartDate,
            command.EndDate,
            command.TimeZoneId,
            schedules,
            now,
            command.ClinicalAttentionId);

        await _commit.SaveAsync(
            plan,
            cancellationToken);

        return MedicationPlanData.From(plan);
    }
}
