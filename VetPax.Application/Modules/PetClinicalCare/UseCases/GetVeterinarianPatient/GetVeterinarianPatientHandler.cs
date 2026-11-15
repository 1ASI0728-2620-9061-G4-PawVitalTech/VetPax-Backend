
using VetPax.Application.Modules.PetClinicalCare.Ports;
using VetPax.Domain.Modules.PetClinicalCare.Repositories;
using VetPax.Domain.Modules.PetClinicalCare.ValueObjects;

namespace VetPax.Application.Modules.PetClinicalCare
    .UseCases.GetVeterinarianPatient;

public sealed record GetVeterinarianPatientResult(
    Guid PetId,
    string Name,
    string Species,
    DateOnly BirthDate,
    string? Breed,
    string? Sex,
    DateTime RegisteredAt,
    DateTime? LastAttentionDate
);

public sealed class GetVeterinarianPatientHandler
{
    private readonly IPetRepository _pets;
    private readonly IClinicalRecordRepository _records;
    private readonly IClinicalAuthorizationPort _authorization;

    public GetVeterinarianPatientHandler(
        IPetRepository pets,
        IClinicalRecordRepository records,
        IClinicalAuthorizationPort authorization)
    {
        _pets = pets;
        _records = records;
        _authorization = authorization;
    }

    public async Task<GetVeterinarianPatientResult?> HandleAsync(
        Guid veterinarianId,
        Guid clinicId,
        Guid petId,
        CancellationToken cancellationToken = default)
    {
        if (veterinarianId == Guid.Empty ||
            clinicId == Guid.Empty ||
            petId == Guid.Empty)
        {
            throw new ArgumentException(
                "Valid identifiers are required.");
        }

        var authorized =
            await _authorization.GetAuthorizedVeterinarianAsync(
                veterinarianId,
                new ClinicId(clinicId),
                new PetId(petId),
                cancellationToken);

        if (authorized is null)
        {
            throw new UnauthorizedAccessException(
                "Veterinarian is not authorized for this patient.");
        }

        var pet = await _pets.FindByIdAsync(
            new PetId(petId),
            cancellationToken);

        if (pet is null)
            return null;

        var record = await _records.FindByPetIdAsync(
            new PetId(petId),
            cancellationToken);

        var lastAttention = record?
            .GetAttentionsChronologically()
            .Where(a => a.ClinicId.Value == clinicId)
            .Select(a => (DateTime?)a.AttentionDate)
            .Max();

        return new GetVeterinarianPatientResult(
            pet.PetId.Value,
            pet.Name,
            pet.Species.ToString(),
            pet.BirthDate,
            pet.Breed,
            pet.Sex?.ToString(),
            pet.RegisteredAt,
            lastAttention
        );
    }
}
