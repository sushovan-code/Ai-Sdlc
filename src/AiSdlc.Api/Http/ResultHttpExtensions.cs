using AiSdlc.Application.Common;

namespace AiSdlc.Api.Http;

/// <summary>Maps use-case results to HTTP responses; failures become RFC 9457 Problem Details.</summary>
public static class ResultHttpExtensions
{
    private const string CodeExtension = "code";

    public static IResult ToHttpResult<T>(this Result<T> result, Func<T, IResult>? onSuccess = null) =>
        result.IsSuccess
            ? (onSuccess ?? (value => Results.Ok(value)))(result.Value)
            : ToProblem(result.Error);

    public static IResult ToHttpResult(this Result result, Func<IResult>? onSuccess = null) =>
        result.IsSuccess
            ? (onSuccess ?? Results.NoContent)()
            : ToProblem(result.Error);

    private static IResult ToProblem(Error error)
    {
        if (error is { Type: ErrorType.Validation, ValidationErrors: { } fields })
        {
            return Results.ValidationProblem(
                fields.ToDictionary(f => f.Key, f => f.Value),
                title: error.Message,
                extensions: new Dictionary<string, object?> { [CodeExtension] = error.Code });
        }

        return Results.Problem(
            title: error.Message,
            statusCode: StatusFor(error.Type),
            extensions: new Dictionary<string, object?> { [CodeExtension] = error.Code });
    }

    private static int StatusFor(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Unavailable => StatusCodes.Status503ServiceUnavailable,
        _ => StatusCodes.Status500InternalServerError,
    };
}
