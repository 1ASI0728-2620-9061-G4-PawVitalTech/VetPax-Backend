using VetPax.Domain.Modules.PetClinicalCare.Repositories;

namespace VetPax.Application.Modules.PetClinicalCare.UseCases.GetClinicalHistory;

public sealed class GetClinicalHistoryHandler
{
    private readonly IPetRepository _petRepository;
    private readonly IClinicalRecordRepository _recordRepository;

    public GetClinicalHistoryHandler(
        IPetRepository petRepository,
        IClinicalRecordRepository recordRepository)
    {
        _petRepository = petRepository;
        _recordRepository = recordRepository;
    }

    public async Task<GetClinicalHistoryResult?> HandleAsync(
        GetClinicalHistoryQuery query,
        CancellationToken cancellationToken = default)
    {
        var pet = await _petRepository.FindByIdAsync(
            query.PetId, cancellationToken);

        if (pet is null)
            return null;

        if (!pet.IsOwnedBy(query.RequesterOwnerId))
            throw new UnauthorizedAccessException(
                "Access denied.");

        var record = await _recordRepository.FindByPetIdAsync(
            query.PetId, cancellationToken);

        if (record is null)
            return null;

        var attentions = record.GetAttentionsChronologically()
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
                    )).ToArray()
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