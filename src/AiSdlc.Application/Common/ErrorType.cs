namespace AiSdlc.Application.Common;

public enum ErrorType
{
    Validation,
    Unauthorized,
    Forbidden,
    NotFound,
    Conflict,

    /// <summary>A service this one depends on (such as the AI model) could not do the work right now.</summary>
    Unavailable,
}
