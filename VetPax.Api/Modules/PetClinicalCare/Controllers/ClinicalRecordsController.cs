using Microsoft.AspNetCore.Mvc;
using VetPax.Api.Modules.PetClinicalCare.Resources;
using VetPax.Application.Modules.PetClinicalCare.UseCases.GetClinicalHistory;
using VetPax.Domain.Modules.PetClinicalCare.ValueObjects;
using VetPax.Application.Modules.PetClinicalCare.UseCases.RegisterClinicalAttention;

namespace VetPax.Api.Modules.PetClinicalCare.Controllers;

[ApiController]
[Route("api/v1/pets/{petId:guid}/clinical-records")]
public sealed class ClinicalRecordsController : ControllerBase
{
    private readonly GetClinicalHistoryHandler _handler;
    private readonly IWebHostEnvironment _environment;
    private readonly RegisterClinicalAttentionHandler _registerHandler;

    
    public ClinicalRecordsController(
        GetClinicalHistoryHandler handler,
        RegisterClinicalAttentionHandler registerHandler,
        IWebHostEnvironment environment)
    {
        _handler = handler;
        _registerHandler = registerHandler;
        _environment = environment;
    }


    [HttpGet]
    [ProducesResponseType(
        typeof(ClinicalHistoryResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHistory(
        Guid petId,
        [FromHeader(Name = "X-Dev-Owner-Id")] string? devOwnerId,
        CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment())
            return StatusCode(403, new
            {
                message = "Authentication integration is required."
            });

        if (petId == Guid.Empty)
            return BadRequest(new
            {
                message = "A valid pet ID is required."
            });

        if (!Guid.TryParse(devOwnerId, out var ownerGuid)
            || ownerGuid == Guid.Empty)
            return BadRequest(new
            {
                message = "A valid X-Dev-Owner-Id is required."
            });

        try
        {
            var query = new GetClinicalHistoryQuery(
                new PetId(petId),
                new OwnerId(ownerGuid)
            );

            var result = await _handler.HandleAsync(
                query, cancellationToken);

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
    [ProducesResponseType(
        typeof(RegisterClinicalAttentionResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RegisterAttention(
        Guid petId,
        [FromBody] RegisterClinicalAttentionRequest request,
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
