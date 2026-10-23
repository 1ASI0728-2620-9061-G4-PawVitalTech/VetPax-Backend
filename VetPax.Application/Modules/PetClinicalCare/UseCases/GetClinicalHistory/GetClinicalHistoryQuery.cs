using VetPax.Domain.Modules.PetClinicalCare.ValueObjects;

namespace VetPax.Application.Modules.PetClinicalCare.UseCases.GetClinicalHistory;

public sealed record GetClinicalHistoryQuery(
    PetId PetId,
    OwnerId RequesterOwnerId
);