using VetPax.Application.Common.Ports;
using VetPax.Domain.Modules.PetClinicalCare.Factories;
using VetPax.Domain.Modules.PetClinicalCare.Repositories;

namespace VetPax.Application.Modules.PetClinicalCare.UseCases.RegisterPet;

public sealed class RegisterPetHandler
{
    private readonly IPetRepository _petRepository;
    private readonly IClinicalRecordRepository _clinicalRecordRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterPetHandler(
        IPetRepository petRepository,
        IClinicalRecordRepository clinicalRecordRepository,
        IUnitOfWork unitOfWork)
    {
        _petRepository = petRepository;
        _clinicalRecordRepository = clinicalRecordRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<RegisterPetResult> HandleAsync(
        RegisterPetCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Create pet aggregate
        var pet = PetFactory.Register(
            command.OwnerId,
            command.Name,
            command.Species,
            command.BirthDate,
            command.Breed,
            command.Sex
        );

        // Create an empty clinical record
        var clinicalRecord =
            ClinicalRecordFactory.OpenFor(pet.PetId);

        // Stage both aggregates for persistence
        await _petRepository.SaveAsync(
            pet,
            cancellationToken
        );

        await _clinicalRecordRepository.SaveAsync(
            clinicalRecord,
            cancellationToken
        );

        // Persist both aggregates atomically
        await _unitOfWork.CommitAsync(
            cancellationToken
        );

        return new RegisterPetResult(
            pet.PetId,
            clinicalRecord.ClinicalRecordId
        );
    }
}