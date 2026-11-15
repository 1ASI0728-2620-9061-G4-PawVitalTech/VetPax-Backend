
using VetPax.Application.Modules.PetClinicalCare.Ports;
using VetPax.Domain.Modules.PetClinicalCare.Repositories;
using VetPax.Domain.Modules.PetClinicalCare.ValueObjects;

namespace VetPax.Application.Modules.PetClinicalCare
    .UseCases.GetClinicalHistory;

public sealed record GetVeterinarianClinicalHistoryQuery(
    PetId PetId,
    AccessRequester Requester
);

public sealed class GetVeterinarianClinicalHistoryHandler
{
    private readonly IClinicalRecordRepository _records;
    private readonly IClinicalAuthorizationPort _authorization;

    public GetVeterinarianClinicalHistoryHandler(
        IClinicalRecordRepository records,
        IClinicalAuthorizationPort authorization)
    {
        _records = records;
        _authorization = authorization;
    }

    public async Task<GetClinicalHistoryResult?> HandleAsync(
        GetVeterinarianClinicalHistoryQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var requester = query.Requester;

        if (requester is null ||
            !requester.IsVeterinarian() ||
            requester.UserId == Guid.Empty ||
            requester.ClinicId is not { } clinicId ||
            clinicId.Value == Guid.Empty ||
            query.PetId.Value == Guid.Empty)
        {
            throw new UnauthorizedAccessException(
                "Valid veterinarian and clinic are required.");
        }

        // Verificar la relación activa entre:
        // veterinario, clínica y mascota.
        var authorizedVeterinarian =
            await _authorization.GetAuthorizedVeterinarianAsync(
                requester.UserId,
                clinicId,
                query.PetId,
                cancellationToken
            );

        if (authorizedVeterinarian is null)
        {
            throw new UnauthorizedAccessException(
                "Veterinarian is not authorized for this patient.");
        }

        // Solo consultar los datos después de autorizar.
        var record = await _records.FindByPetIdAsync(
            query.PetId,
            cancellationToken
        );

        if (record is null)
            return null;

        var attentions = record.GetAttentionsChronologically()
            .Where(a => a.ClinicId.Value == clinicId.Value)
            .Select(a => new ClinicalAttentionResult(
                a.AttentionId.Value,
                a.ClinicId.Value,
                a.VeterinarianId.Value,
                a.AttentionDate,
                a.Reason,
                a.Diagnosis,
                a.TreatmentSummary,
                a.Observations,
                a.Indicators.Select(i =>
                    new ClinicalIndicatorResult(
                        i.Name,
                        i.Value,
                        i.Unit
                    )
                ).ToArray()
            ))
            .ToArray();

        return new GetClinicalHistoryResult(
            record.PetId.Value,
            record.ClinicalRecordId.Value,
            record.LastUpdatedAt,
            attentions
        );
    }
}
