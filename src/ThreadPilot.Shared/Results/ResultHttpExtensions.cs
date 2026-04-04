using Microsoft.AspNetCore.Http;
using ThreadPilot.Shared.Results;

public static class ResultHttpExtensions
{
    public static IResult ToHttpResult(this Result result)
    {
        if (result.IsSuccess)
            return Results.Ok();

        return MapError(result.Error);
    }

    public static IResult ToHttpResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
            return Results.Ok(result.Value);

        return MapError(result.Error);
    }

    private static IResult MapError(Error? error)
    {
        if (error is null)
            return Results.Problem("Unknown error.");

        return error.Code switch
        {
            "invalid_input" => Results.BadRequest(error),
            "validation_error" => Results.BadRequest(error),
            "not_found" => Results.NotFound(error),
            "dependency_failure" => Results.StatusCode(503),
            _ => Results.Problem(error.Message)
        };
    }
}