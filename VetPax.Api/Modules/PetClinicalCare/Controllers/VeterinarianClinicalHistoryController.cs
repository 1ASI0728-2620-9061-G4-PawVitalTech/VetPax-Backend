
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetPax.Api.Modules.PetClinicalCare.Resources;
using VetPax.Application.Modules.IdentityAccessManagement.Ports;
using VetPax.Application.Modules.PetClinicalCare.UseCases.GetClinicalHistory;
using VetPax.Domain.Modules.PetClinicalCare.ValueObjects;

namespace VetPax.Api.Modules.PetClinicalCare.Controllers;

[ApiController]
[Authorize(Roles = "veterinarian")]
[Route("api/v1/pets/{petId:guid}/clinical-records/veterinarian")]
public sealed class VeterinarianClinicalHistoryController
    : ControllerBase
{
    private readonly GetVeterinarianClinicalHistoryHandler _handler;
    private readonly IAuthenticatedUserPort _currentUser;

    public VeterinarianClinicalHistoryController(
        GetVeterinarianClinicalHistoryHandler handler,
        IAuthenticatedUserPort currentUser)
    {
        _handler = handler;
        _currentUser = currentUser;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(ClinicalHistoryResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHistory(
        Guid petId,
        [FromQuery] Guid clinicId,
        CancellationToken cancellationToken)
    {
        if (petId == Guid.Empty || clinicId == Guid.Empty)
        {
            return BadRequest(new
            {
                message = "Valid pet and clinic IDs are required."
            });
        }

        var identity = await _currentUser.GetCurrentAsync(
            cancellationToken
        );

        if (identity is null)
            return Unauthorized();

        try
        {
            var requester = new AccessRequester(
                identity.UserId.Value,
                RequesterRole.Veterinarian,
                new ClinicId(clinicId)
            );

            var query = new GetVeterinarianClinicalHistoryQuery(
                new PetId(petId),
                requester
            );

            var result = await _handler.HandleAsync(
                query,
                cancellationToken
            );

            if (result is null)
            {
                return NotFound(new
                {
                    message = "Clinical record not found."
                });
            }

            return Ok(new ClinicalHistoryResponse(
                result.PetId,
                result.ClinicalRecordId,
                result.LastUpdatedAt,
                result.Attentions.Count == 0
                    ? "No clinical attentions have been recorded yet."
                    : null,
                result.Attentions
            ));
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(403, new
            {
                message = "Access denied."
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }
}
