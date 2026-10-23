
using Microsoft.AspNetCore.Mvc;
using VetPax.Api.Modules.PetClinicalCare.Resources;
using VetPax.Application.Modules.PetClinicalCare.UseCases.GetClinicalEvolution;
using VetPax.Domain.Modules.PetClinicalCare.ValueObjects;

namespace VetPax.Api.Modules.PetClinicalCare.Controllers;

[ApiController]
[Route("api/v1/pets/{petId:guid}/evolution")]
public sealed class ClinicalEvolutionController : ControllerBase
{
    private readonly GetClinicalEvolutionHandler _handler;
    private readonly IWebHostEnvironment _environment;

    public ClinicalEvolutionController(
        GetClinicalEvolutionHandler handler,
        IWebHostEnvironment environment)
    {
        _handler = handler;
        _environment = environment;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(ClinicalEvolutionResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEvolution(
        Guid petId,
        [FromHeader(Name = "X-Dev-User-Id")] string? devUserId,
        [FromHeader(Name = "X-Dev-Clinic-Id")] string? devClinicId,
        CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment())
            return StatusCode(403, new
            {
                message = "Authentication integration is required."
            });

        if (petId == Guid.Empty ||
            !Guid.TryParse(devUserId, out var userId) ||
            userId == Guid.Empty ||
            !Guid.TryParse(devClinicId, out var clinicId) ||
            clinicId == Guid.Empty)
        {
            return BadRequest(new
            {
                message = "Valid pet, user and clinic IDs are required."
            });
        }

        try
        {
            var requester = new AccessRequester(
                userId,
                RequesterRole.Veterinarian,
                new ClinicId(clinicId)
            );

            var query = new GetClinicalEvolutionQuery(
                new PetId(petId),
                requester
            );

            var result = await _handler.HandleAsync(
                query,
                cancellationToken
            );

            var entries = result.Entries
                .Select(e => new EvolutionEntryResponse(
                    e.AttentionDate,
                    e.IndicatorName,
                    e.Value,
                    e.Unit
                ))
                .ToArray();

            return Ok(new ClinicalEvolutionResponse(
                result.PetId,
                result.HasSufficientData,
                result.HasSufficientData
                    ? null
                    : "Insufficient comparable clinical data.",
                entries
            ));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(403, new
            {
                message = "Access denied."
            });
        }
    }
}
