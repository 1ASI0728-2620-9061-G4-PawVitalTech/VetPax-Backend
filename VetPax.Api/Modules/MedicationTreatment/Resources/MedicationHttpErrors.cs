
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using VetPax.Application.Modules.MedicationTreatment.Exceptions;
using VetPax.Domain.Modules.MedicationTreatment.Exceptions;

namespace VetPax.Api.Modules.MedicationTreatment.Resources;

public static class MedicationHttpErrors
{
    public static bool CanHandle(Exception exception)
    {
        return exception switch
        {
            UnauthorizedAccessException => true,
            KeyNotFoundException => true,
            MedicationValidationException => true,
            MedicationConflictException => true,
            MedicationDomainException => true,
            ArgumentException => true,
            DbUpdateConcurrencyException => true,
            DbUpdateException ex
                when IsDuplicateKey(ex) => true,
            _ => false
        };
    }

    public static IActionResult Map(
        ControllerBase controller,
        Exception exception)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(exception);

        switch (exception)
        {
            case UnauthorizedAccessException:
                return controller.User.Identity?.IsAuthenticated == true
                    ? controller.Forbid()
                    : controller.Challenge();

            case KeyNotFoundException:
                return Problem(
                    StatusCodes.Status404NotFound,
                    "Resource not found",
                    exception.Message);

            case MedicationValidationException:
            case ArgumentException:
                return Problem(
                    StatusCodes.Status400BadRequest,
                    "Invalid medication request",
                    exception.Message);

            case MedicationConflictException:
                return Problem(
                    StatusCodes.Status409Conflict,
                    "Medication conflict",
                    exception.Message);

            case MedicationDomainException domainException:
                return IsDomainConflict(domainException)
                    ? Problem(
                        StatusCodes.Status409Conflict,
                        "Medication conflict",
                        domainException.Message)
                    : Problem(
                        StatusCodes.Status400BadRequest,
                        "Invalid medication operation",
                        domainException.Message);

            case DbUpdateConcurrencyException:
                return Problem(
                    StatusCodes.Status409Conflict,
                    "Concurrent modification",
                    "The medication data was modified by another request.");

            case DbUpdateException dbException
                when IsDuplicateKey(dbException):
                return Problem(
                    StatusCodes.Status409Conflict,
                    "Duplicate medication operation",
                    "The requested medication record already exists.");

            default:
                throw exception;
        }
    }

    private static bool IsDomainConflict(
        MedicationDomainException exception)
    {
        var message = exception.Message;

        return message.Contains(
                   "already been administered",
                   StringComparison.OrdinalIgnoreCase)
               || message.Contains(
                   "not active",
                   StringComparison.OrdinalIgnoreCase)
               || message.Contains(
                   "Only pending",
                   StringComparison.OrdinalIgnoreCase)
               || message.Contains(
                   "already exists",
                   StringComparison.OrdinalIgnoreCase)
               || message.Contains(
                   "has a different UTC instant",
                   StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsDuplicateKey(
        DbUpdateException exception)
    {
        return exception.InnerException is MySqlException
        {
            Number: 1062
        };
    }

    private static ObjectResult Problem(
        int status,
        string title,
        string detail)
    {
        return new ObjectResult(new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail
        })
        {
            StatusCode = status,
            ContentTypes =
            {
                "application/problem+json"
            }
        };
    }
}
