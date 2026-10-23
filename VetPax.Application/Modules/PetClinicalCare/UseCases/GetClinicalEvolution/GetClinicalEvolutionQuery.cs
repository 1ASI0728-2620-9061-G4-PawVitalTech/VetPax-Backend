using VetPax.Domain.Modules.PetClinicalCare.ValueObjects;

namespace VetPax.Application.Modules.PetClinicalCare.UseCases.GetClinicalEvolution;

public sealed record GetClinicalEvolutionQuery(
    PetId PetId,
    AccessRequester Requester
);