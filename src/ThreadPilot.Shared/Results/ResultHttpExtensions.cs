using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using HttpResults = Microsoft.AspNetCore.Http.Results;

namespace ThreadPilot.Shared.Results;

public static class ResultHttpExtensions
{
    public static IResult ToHttpResult(this Result result)
    {
        if (result.IsSuccess)
            return HttpResults.Ok();

        return MapProblemDetails(result.Error);
    }

    public static IResult ToHttpResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
            return HttpResults.Ok(result.Value);

        return MapProblemDetails(result.Error);
    }

    private static IResult MapProblemDetails(Error? error)
    {
        if (error is null)
        {
            return HttpResults.Problem(new ProblemDetails
            {
                Title = "Unknown error",
                Status = StatusCodes.Status500InternalServerError,
                Detail = "An unknown error occurred."
            });
        }

        var (status, title) = error.Code switch
        {
            "invalid_input" => (StatusCodes.Status400BadRequest, "Invalid input"),
            "validation_error" => (StatusCodes.Status400BadRequest, "Validation error"),
            "not_found" => (StatusCodes.Status404NotFound, "Not found"),
            "dependency_failure" => (StatusCodes.Status503ServiceUnavailable, "Dependency failure"),
            _ => (StatusCodes.Status500InternalServerError, "Internal server error")
        };

        var problem = new ProblemDetails
        {
            Title = title,
            Status = status,
            Detail = error.Message,
            Type = $"https://httpstatuses.com/{status}"
        };

        problem.Extensions["errorCode"] = error.Code;

        return HttpResults.Problem(problem);
    }
}
