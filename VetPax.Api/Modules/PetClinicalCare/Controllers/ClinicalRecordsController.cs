
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetPax.Api.Modules.PetClinicalCare.Resources;
using VetPax.Application.Modules.IdentityAccessManagement.Ports;
using VetPax.Application.Modules.PetClinicalCare.UseCases.GetClinicalHistory;
using VetPax.Application.Modules.PetClinicalCare.UseCases.RegisterClinicalAttention;
using VetPax.Domain.Modules.PetClinicalCare.ValueObjects;

namespace VetPax.Api.Modules.PetClinicalCare.Controllers;

[ApiController]
[Route("api/v1/pets/{petId:guid}/clinical-records")]
public sealed class ClinicalRecordsController : ControllerBase
{
    private readonly GetClinicalHistoryHandler _handler;
    private readonly RegisterClinicalAttentionHandler _registerHandler;
    private readonly IAuthenticatedUserPort _currentUser;
    private readonly IWebHostEnvironment _environment;

    public ClinicalRecordsController(
        GetClinicalHistoryHandler handler,
        RegisterClinicalAttentionHandler registerHandler,
        IAuthenticatedUserPort currentUser,
        IWebHostEnvironment environment)
    {
        _handler = handler;
        _registerHandler = registerHandler;
        _currentUser = currentUser;
        _environment = environment;
    }

    [HttpGet]
    [Authorize(Roles = "owner")]
    [ProducesResponseType(
        typeof(ClinicalHistoryResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHistory(
        Guid petId,
        CancellationToken cancellationToken)
    {
        if (petId == Guid.Empty)
            return BadRequest(new
            {
                message = "A valid pet ID is required."
            });

        var identity = await _currentUser.GetCurrentAsync(
            cancellationToken);

        if (identity is null)
            return Unauthorized();

        try
        {
            var query = new GetClinicalHistoryQuery(
                new PetId(petId),
                new OwnerId(identity.UserId.Value)
            );

            var result = await _handler.HandleAsync(
                query,
                cancellationToken
            );

            if (result is null)
                return NotFound(new
                {
                    message = "Pet or clinical record not found."
                });

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
    }

    [HttpPost]
    [Authorize(Roles = "veterinarian")]
    [ProducesResponseType(
        typeof(RegisterClinicalAttentionResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RegisterAttention(
        Guid petId,
        [FromBody] RegisterClinicalAttentionRequest request,
        [FromHeader(Name = "X-Dev-Clinic-Id")] string? devClinicId,
        CancellationToken cancellationToken)
    {
        // US03: clinic authorization integration pending.
        if (!_environment.IsDevelopment())
            return StatusCode(403, new
            {
                message = "Clinical authorization integration is required."
            });

        var identity = await _currentUser.GetCurrentAsync(
            cancellationToken);

        if (identity is null)
            return Unauthorized();

        if (petId == Guid.Empty ||
            !Guid.TryParse(devClinicId, out var clinicId) ||
            clinicId == Guid.Empty)
        {
            return BadRequest(new
            {
                message = "Valid pet and clinic IDs are required."
            });
        }

        try
        {
            var requester = new AccessRequester(
                identity.UserId.Value,
                RequesterRole.Veterinarian,
                new ClinicId(clinicId)
            );

            var command = new RegisterClinicalAttentionCommand(
                new PetId(petId),
                requester,
                request.AttentionDate,
                request.Reason,
                request.Diagnosis,
                request.TreatmentSummary,
                request.Observations,
                request.Indicators?.Select(i =>
                    new ClinicalIndicatorInput(
                        i.Name,
                        i.Value,
                        i.Unit
                    )).ToArray()
            );

            var result = await _registerHandler.HandleAsync(
                command,
                cancellationToken
            );

            var response = new RegisterClinicalAttentionResponse(
                result.AttentionId,
                result.ClinicalRecordId,
                result.PetId
            );

            return Created(
                $"/api/v1/pets/{petId}/clinical-records",
                response
            );
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
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
