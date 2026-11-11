using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace AiSdlc.Api.Http;

/// <summary>
/// Turns client mistakes that surface as exceptions (a body over the size limit, a malformed form) into the 4xx the
/// client earned, instead of a generic 500. Everything else still falls through to the default handler.
/// </summary>
public sealed class ClientErrorExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    private const string MultipartLimitText = "Multipart body length limit";

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var status = StatusFor(exception);
        if (status is null)
        {
            return false;
        }

        httpContext.Response.StatusCode = status.Value;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = status == StatusCodes.Status413PayloadTooLarge
                    ? "The upload is too large."
                    : "The request could not be read.",
            },
        });
    }

    private static int? StatusFor(Exception exception) => exception switch
    {
        BadHttpRequestException bad => bad.StatusCode,
        InvalidDataException invalid when invalid.Message.Contains(MultipartLimitText, StringComparison.Ordinal)
            => StatusCodes.Status413PayloadTooLarge,
        InvalidDataException => StatusCodes.Status400BadRequest,
        _ => null,
    };
}
