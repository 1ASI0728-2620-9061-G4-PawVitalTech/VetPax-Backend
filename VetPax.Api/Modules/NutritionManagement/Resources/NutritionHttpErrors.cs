
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using VetPax.Domain.Modules.NutritionManagement.Exceptions;

namespace VetPax.Api.Modules.NutritionManagement.Resources;

public static class NutritionHttpErrors
{
    public static bool IsExpected(Exception exception)
    {
        return exception is
            NutritionDomainException or
            ArgumentException or
            KeyNotFoundException or
            UnauthorizedAccessException or
            InvalidOperationException or
            DbUpdateConcurrencyException
            || IsDuplicateKey(exception);
    }

    public static IActionResult ToActionResult(
        ControllerBase controller,
        Exception exception)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(exception);

        return exception switch
        {
            NutritionDomainException ex =>
                controller.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid feeding plan",
                    detail: ex.Message),

            ArgumentException ex =>
                controller.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid request",
                    detail: ex.Message),

            KeyNotFoundException =>
                controller.Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Feeding plan not found"),

            UnauthorizedAccessException =>
                controller.Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Access denied"),

            DbUpdateConcurrencyException =>
                controller.Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Concurrent feeding plan modification",
                    detail: "Reload the feeding plan and retry."),

            DbUpdateException ex when IsDuplicateKey(ex) =>
                controller.Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Feeding plan conflict",
                    detail: "A conflicting record already exists."),

            InvalidOperationException ex =>
                controller.Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Feeding plan conflict",
                    detail: ex.Message),

            _ => throw new ArgumentException(
                "An unexpected exception was passed to NutritionHttpErrors.",
                nameof(exception))
        };
    }

    private static bool IsDuplicateKey(Exception exception)
    {
        return exception is DbUpdateException
        {
            InnerException: MySqlException
            {
                Number: 1062
            }
        };
    }
}
