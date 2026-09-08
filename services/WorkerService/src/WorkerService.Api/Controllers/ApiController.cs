using CleanArchitecture.BuildingBlocks;
using Microsoft.AspNetCore.Mvc;

namespace WorkerService.Api.Controllers;

[ApiController]
public abstract class ApiController : ControllerBase
{
    protected IActionResult Match<T>(Result<T> result) =>
        result.IsSuccess ? Ok(result.Value) : Problem(result);

    protected IActionResult Match(Result result) =>
        result.IsSuccess ? NoContent() : Problem(result);

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
}
