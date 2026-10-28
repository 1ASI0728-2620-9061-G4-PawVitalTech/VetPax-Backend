
using VetPax.Application.Modules.PetClinicalCare.Ports;
using VetPax.Domain.Modules.PetClinicalCare.Repositories;
using VetPax.Domain.Modules.PetClinicalCare.Services;

namespace VetPax.Application.Modules.PetClinicalCare.UseCases.GetClinicalEvolution;

public sealed class GetClinicalEvolutionHandler
{
    private readonly IPetRepository _pets;
    private readonly IClinicalRecordRepository _records;
    private readonly IClinicalAuthorizationPort _authorization;
    private readonly ClinicalEvolutionService _evolutionService;

    public GetClinicalEvolutionHandler(
        IPetRepository pets,
        IClinicalRecordRepository records,
        IClinicalAuthorizationPort authorization,
        ClinicalEvolutionService evolutionService)
    {
        _pets = pets;
        _records = records;
        _authorization = authorization;
        _evolutionService = evolutionService;
    }

    public async Task<GetClinicalEvolutionResult> HandleAsync(
        GetClinicalEvolutionQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var requester = query.Requester;

        if (requester is null ||
            !requester.IsVeterinarian() ||
            requester.UserId == Guid.Empty ||
            requester.ClinicId is not { } clinicId ||
            clinicId.Value == Guid.Empty)
        {
            throw new UnauthorizedAccessException(
                "Veterinarian authentication is required.");
        }

        var pet = await _pets.FindByIdAsync(
            query.PetId,
            cancellationToken);

        if (pet is null)
        {
            throw new KeyNotFoundException(
                "Pet not found.");
        }

        var authorized =
            await _authorization.CanViewEvolutionAsync(
                requester.UserId,
                clinicId,
                query.PetId,
                cancellationToken);

        if (!authorized)
        {
            throw new UnauthorizedAccessException(
                "Veterinarian is not authorized for this patient.");
        }

        var record = await _records.FindByPetIdAsync(
            query.PetId,
            cancellationToken);

        if (record is null)
        {
            throw new KeyNotFoundException(
                "Clinical record not found.");
        }

        var evolution = _evolutionService.BuildEvolution(
            record);

        return new GetClinicalEvolutionResult(
            evolution.PetId.Value,
            evolution.HasSufficientData(),
            evolution.Entries
        );
    }
}
