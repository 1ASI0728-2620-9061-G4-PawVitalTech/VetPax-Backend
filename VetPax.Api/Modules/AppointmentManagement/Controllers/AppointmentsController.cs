
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using VetPax.Api.Modules.AppointmentManagement.Resources;
using VetPax.Application.Modules.AppointmentManagement.Exceptions;
using VetPax.Application.Modules.AppointmentManagement.UseCases.CancelAppointment;
using VetPax.Application.Modules.AppointmentManagement.UseCases.GetAppointment;
using VetPax.Application.Modules.AppointmentManagement.UseCases.GetVeterinarianAgenda;
using VetPax.Application.Modules.AppointmentManagement.UseCases.MarkAppointmentAttended;
using VetPax.Application.Modules.AppointmentManagement.UseCases.RescheduleAppointment;
using VetPax.Application.Modules.AppointmentManagement.UseCases.ScheduleAppointment;
using VetPax.Domain.Modules.AppointmentManagement.Exceptions;

namespace VetPax.Api.Modules.AppointmentManagement.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/appointments")]
public sealed class AppointmentsController : ControllerBase
{
    private readonly ScheduleAppointmentHandler _schedule;
    private readonly RescheduleAppointmentHandler _reschedule;
    private readonly CancelAppointmentHandler _cancel;
    private readonly MarkAppointmentAttendedHandler _attend;
    private readonly GetVeterinarianAgendaHandler _agenda;
    private readonly GetAppointmentHandler _get;

    public AppointmentsController(
        ScheduleAppointmentHandler schedule,
        RescheduleAppointmentHandler reschedule,
        CancelAppointmentHandler cancel,
        MarkAppointmentAttendedHandler attend,
        GetVeterinarianAgendaHandler agenda,
        GetAppointmentHandler get)
    {
        _schedule = schedule;
        _reschedule = reschedule;
        _cancel = cancel;
        _attend = attend;
        _agenda = agenda;
        _get = get;
    }

    // -------------------------------------------------
    // US04 — Agendar una cita veterinaria
    // -------------------------------------------------

    [HttpPost]
    [Authorize(Roles = "owner")]
    [ProducesResponseType(
        typeof(AppointmentResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Schedule(
        [FromBody] ScheduleAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        return ExecuteAsync(async () =>
        {
            var command = new ScheduleAppointmentCommand(
                request.PetId,
                request.ClinicId,
                request.VeterinarianId,
                request.Date,
                request.StartTime,
                request.Reason
            );

            var result = await _schedule.HandleAsync(
                command,
                cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new
                {
                    appointmentId = result.AppointmentId
                },
                AppointmentResponse.From(result));
        });
    }

    // -------------------------------------------------
    // US05 / US06 — Modificar una cita
    // -------------------------------------------------

    [HttpPatch("{appointmentId:guid}")]
    [Authorize(Roles = "owner,veterinarian")]
    [ProducesResponseType(
        typeof(AppointmentResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Patch(
        Guid appointmentId,
        [FromBody] PatchAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        if (appointmentId == Guid.Empty)
        {
            return Task.FromResult<IActionResult>(
                BadRequest(new
                {
                    message = "A valid appointment ID is required."
                }));
        }

        return ExecuteAsync(async () =>
        {
            var operation = request.Operation?
                .Trim()
                .ToLowerInvariant();

            switch (operation)
            {
                // US05: cancelar.
                case "cancel":
                {
                    var result = await _cancel.HandleAsync(
                        new CancelAppointmentCommand(
                            appointmentId),
                        cancellationToken);

                    return Ok(
                        AppointmentResponse.From(result));
                }

                // US05: reprogramar.
                case "reschedule":
                {
                    if (request.Date is null ||
                        request.StartTime is null)
                    {
                        return BadRequest(new
                        {
                            message =
                                "Date and startTime are required."
                        });
                    }

                    var result = await _reschedule.HandleAsync(
                        new RescheduleAppointmentCommand(
                            appointmentId,
                            request.Date.Value,
                            request.StartTime.Value),
                        cancellationToken);

                    return Ok(
                        AppointmentResponse.From(result));
                }

                // US06: marcar como atendida.
                case "mark_attended":
                {
                    if (request.ClinicalAttentionId is null ||
                        request.ClinicalAttentionId == Guid.Empty)
                    {
                        return BadRequest(new
                        {
                            message =
                                "A valid clinicalAttentionId is required."
                        });
                    }

                    var result = await _attend.HandleAsync(
                        new MarkAppointmentAttendedCommand(
                            appointmentId,
                            request.ClinicalAttentionId.Value),
                        cancellationToken);

                    return Ok(
                        AppointmentResponse.From(result));
                }

                default:
                    return BadRequest(new
                    {
                        message =
                            "Operation must be cancel, " +
                            "reschedule or mark_attended."
                    });
            }
        });
    }

    // -------------------------------------------------
    // TS03 — Consultar una cita individual
    // -------------------------------------------------

    [HttpGet("{appointmentId:guid}")]
    [Authorize(Roles = "owner,veterinarian")]
    [ProducesResponseType(
        typeof(AppointmentResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetById(
        Guid appointmentId,
        CancellationToken cancellationToken)
    {
        return ExecuteAsync(async () =>
        {
            var result = await _get.HandleAsync(
                new GetAppointmentQuery(appointmentId),
                cancellationToken);

            return Ok(
                AppointmentResponse.From(result));
        });
    }

    // -------------------------------------------------
    // US06 — Consultar agenda del veterinario
    // -------------------------------------------------

    [HttpGet("agenda")]
    [Authorize(Roles = "veterinarian")]
    [ProducesResponseType(
        typeof(AppointmentAgendaResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public Task<IActionResult> GetAgenda(
        [FromQuery] Guid clinicId,
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken)
    {
        return ExecuteAsync(async () =>
        {
            var result = await _agenda.HandleAsync(
                new GetVeterinarianAgendaQuery(
                    clinicId,
                    from,
                    to),
                cancellationToken);

            var response = new AppointmentAgendaResponse(
                clinicId,
                from,
                to,
                result
                    .Select(AgendaAppointmentResponse.From)
                    .ToArray()
            );

            return Ok(response);
        });
    }

    // -------------------------------------------------
    // Traducción centralizada de errores a HTTP
    // -------------------------------------------------

    private async Task<IActionResult> ExecuteAsync(
        Func<Task<IActionResult>> action)
    {
        try
        {
            return await action();
        }
        catch (AppointmentConflictException ex)
        {
            return Conflict(new
            {
                message = ex.Message
            });
        }
        catch (AppointmentStateException ex)
        {
            return Conflict(new
            {
                message = ex.Message
            });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new
            {
                message =
                    "Appointment was modified concurrently. " +
                    "Please reload and try again."
            });
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is MySqlException
            {
                Number: 1062
            })
        {
            // MySQL: ER_DUP_ENTRY (1062).
            // La base de datos rechazó una clave única.
            return Conflict(new
            {
                message =
                    "The requested appointment slot is unavailable."
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new
            {
                message = ex.Message
            });
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new
                {
                    message = "Access denied."
                });
        }
    }
}
