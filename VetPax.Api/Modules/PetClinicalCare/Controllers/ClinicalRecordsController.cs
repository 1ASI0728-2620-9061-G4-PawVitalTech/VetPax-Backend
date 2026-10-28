
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using VetPax.Api.Modules.PetClinicalCare.Resources;
using VetPax.Application.Modules.IdentityAccessManagement.Ports;
using VetPax.Application.Modules.PetClinicalCare.UseCases.GetClinicalHistory;
using VetPax.Application.Modules.PetClinicalCare.UseCases.RegisterClinicalAttention;
using VetPax.Domain.Modules.PetClinicalCare.ValueObjects;
using VetPax.Application.Modules.PetClinicalCare.Ports;

namespace VetPax.Api.Modules.PetClinicalCare.Controllers;

[ApiController]
[Route("api/v1/pets/{petId:guid}/clinical-records")]
public sealed class ClinicalRecordsController : ControllerBase
{
    private readonly GetClinicalHistoryHandler _handler;
    private readonly RegisterClinicalAttentionHandler _registerHandler;
    private readonly IAuthenticatedUserPort _currentUser;

    public ClinicalRecordsController(
        GetClinicalHistoryHandler handler,
        RegisterClinicalAttentionHandler registerHandler,
        IAuthenticatedUserPort currentUser)
    {
        _handler = handler;
        _registerHandler = registerHandler;
        _currentUser = currentUser;
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
        {
            return BadRequest(new
            {
                message = "A valid pet ID is required."
            });
        }

        var identity = await _currentUser.GetCurrentAsync(
            cancellationToken);

        if (identity is null)
            return Unauthorized();

        try
        {
            var query = new GetClinicalHistoryQuery(
                new PetId(petId),
                new OwnerId(identity.UserId.Value));

            var result = await _handler.HandleAsync(
                query,
                cancellationToken);

            if (result is null)
            {
                return NotFound(new
                {
                    message = "Pet or clinical record not found."
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
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegisterAttention(
        Guid petId,
        [FromQuery] Guid clinicId,
        [FromQuery] Guid? appointmentId,
        [FromBody] RegisterClinicalAttentionRequest request,
        CancellationToken cancellationToken)
    {
        if (petId == Guid.Empty || clinicId == Guid.Empty ||
            (appointmentId.HasValue &&
             appointmentId.Value == Guid.Empty))
        {
            return BadRequest(new
            {
                message = "Valid pet, clinic and appointment IDs are required."
            });
        }

        var identity = await _currentUser.GetCurrentAsync(
            cancellationToken);

        if (identity is null)
            return Unauthorized();

        try
        {
            var requester = new AccessRequester(
                identity.UserId.Value,
                RequesterRole.Veterinarian,
                new ClinicId(clinicId));

            var command = new RegisterClinicalAttentionCommand(
                new PetId(petId),
                requester,
                request.AttentionDate,
                request.Reason,
                request.Diagnosis,
                request.TreatmentSummary,
                request.Observations,
                request.Indicators?
                    .Select(i => new ClinicalIndicatorInput(
                        i.Name,
                        i.Value,
                        i.Unit))
                    .ToArray()
            );

            var result = appointmentId.HasValue
                ? await _registerHandler.HandleForAppointmentAsync(
                    command,
                    appointmentId.Value,
                    cancellationToken)
                : await _registerHandler.HandleAsync(
                    command,
                    cancellationToken);

            return Created(
                $"/api/v1/pets/{petId}/clinical-records",
                new RegisterClinicalAttentionResponse(
                    result.AttentionId,
                    result.ClinicalRecordId,
                    result.PetId));
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
        catch (ClinicalAppointmentLinkConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is MySqlException
            {
                Number: 1062
            })
        {
            return Conflict(new
            {
                message = "Appointment already has a linked attention."
            });
        }
    }
}
