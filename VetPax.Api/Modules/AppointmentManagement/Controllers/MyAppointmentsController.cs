using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetPax.Api.Modules.AppointmentManagement.Resources;
using VetPax.Application.Modules.AppointmentManagement
    .UseCases.GetMyAppointments;

namespace VetPax.Api.Modules.AppointmentManagement.Controllers;

[ApiController]
[Authorize(Roles = "owner")]
[Route("api/v1/me/appointments")]
public sealed class MyAppointmentsController : ControllerBase
{
    private readonly GetMyAppointmentsHandler _handler;

    public MyAppointmentsController(
        GetMyAppointmentsHandler handler)
    {
        _handler = handler;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(AppointmentResponse[]),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMyAppointments(
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _handler.HandleAsync(
                new GetMyAppointmentsQuery(),
                cancellationToken);

            return Ok(
                result
                    .Select(AppointmentResponse.From)
                    .ToArray());
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