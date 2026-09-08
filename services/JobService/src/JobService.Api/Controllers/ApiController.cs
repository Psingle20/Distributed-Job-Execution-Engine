using CleanArchitecture.BuildingBlocks;
using Microsoft.AspNetCore.Mvc;

namespace JobService.Api.Controllers;

[ApiController]
public abstract class ApiController : ControllerBase
{
    protected IActionResult Match<T>(Result<T> result) =>
        result.IsSuccess ? Ok(result.Value) : Problem(result);

    protected IActionResult Match(Result result) =>
        result.IsSuccess ? NoContent() : Problem(result);

    protected IActionResult MatchCreated<T>(Result<T> result, string actionName, object? routeValues = null) =>
        result.IsSuccess
            ? CreatedAtAction(actionName, routeValues, result.Value)
            : Problem(result);

    private ObjectResult Problem(Result result)
    {
        if (result.IsSuccess)
        {
            throw new InvalidOperationException("Cannot build a problem response from a successful Result.");
        }

        Error error = result.Error;
        int statusCode = GetStatusCode(error.Type);

        var problemDetails = new ProblemDetails
        {
            Title = error.Code,
            Detail = error.Description,
            Type = GetType(error.Type),
            Status = statusCode
        };

        if (error is ValidationError validationError)
        {
            problemDetails.Extensions["errors"] = validationError.Errors;
        }

        return new ObjectResult(problemDetails) { StatusCode = statusCode };
    }

    private static int GetStatusCode(ErrorType errorType) =>
        errorType switch
        {
            ErrorType.Validation or ErrorType.Problem => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

    private static string GetType(ErrorType errorType) =>
        errorType switch
        {
            ErrorType.Validation => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            ErrorType.Problem => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            ErrorType.NotFound => "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            ErrorType.Conflict => "https://tools.ietf.org/html/rfc7231#section-6.5.8",
            _ => "https://tools.ietf.org/html/rfc7231#section-6.6.1"
        };
}
