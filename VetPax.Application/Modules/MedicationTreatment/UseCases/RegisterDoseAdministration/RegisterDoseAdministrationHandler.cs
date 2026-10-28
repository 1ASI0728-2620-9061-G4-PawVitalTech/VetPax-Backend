
using VetPax.Application.Modules.MedicationTreatment.Exceptions;
using VetPax.Application.Modules.MedicationTreatment.Models;
using VetPax.Application.Modules.MedicationTreatment.Ports;
using VetPax.Application.Modules.MedicationTreatment.Services;
using VetPax.Domain.Modules.MedicationTreatment.Repositories;
using VetPax.Domain.Modules.MedicationTreatment.ValueObjects;

namespace VetPax.Application.Modules.MedicationTreatment.UseCases.RegisterDoseAdministration;

public sealed class RegisterDoseAdministrationHandler
{
    private readonly IMedicationPlanRepository _repository;
    private readonly MedicationAuthorizationService _authorization;
    private readonly MedicationTimeService _time;
    private readonly IMedicationReminderPort _reminders;
    private readonly MedicationCommitService _commit;
    private readonly TimeProvider _clock;

    public RegisterDoseAdministrationHandler(
        IMedicationPlanRepository repository,
        MedicationAuthorizationService authorization,
        MedicationTimeService time,
        IMedicationReminderPort reminders,
        MedicationCommitService commit,
        TimeProvider clock)
    {
        _repository = repository;
        _authorization = authorization;
        _time = time;
        _reminders = reminders;
        _commit = commit;
        _clock = clock;
    }

    public async Task<MedicationAdministrationData> HandleAsync(
        RegisterDoseAdministrationCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.DoseId == Guid.Empty)
        {
            throw new MedicationValidationException(
                "A valid dose ID is required.");
        }

        var plan = await _repository.FindByDoseIdAsync(
            new MedicationDoseId(command.DoseId),
            cancellationToken);

        if (plan is null)
            throw new KeyNotFoundException("Medication dose not found.");

        var ownerId = await _authorization.RequirePlanOwnerAsync(
            plan,
            cancellationToken);

        var now = _clock.GetUtcNow().UtcDateTime;

        if (command.AdministeredAtUtc.Kind != DateTimeKind.Utc ||
            command.AdministeredAtUtc > now)
        {
            throw new MedicationValidationException(
                "Administration time must be a valid past or current UTC instant.");
        }

        var today = _time.GetLocalDate(
            now,
            plan.TimeZoneId);

        var administration = plan.RegisterAdministration(
            new MedicationDoseId(command.DoseId),
            new MedicationOwnerId(ownerId),
            command.AdministeredAtUtc,
            now,
            today);

        // Evita enviar un aviso de una dosis que
        // acaba de registrarse como administrada.
        await _reminders.CancelForDoseAsync(
            command.DoseId,
            cancellationToken);

        await _commit.SaveAsync(
            plan,
            cancellationToken);

        return MedicationAdministrationData.From(
            administration);
    }
}
