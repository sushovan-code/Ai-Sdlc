namespace AiSdlc.Application.Common;

/// <summary>An expected failure (validation, not found, business rule). Mapped to Problem Details at the edge.</summary>
public sealed record Error(string Code, string Message, ErrorType Type)
{
    public const string ValidationFailedCode = "validation.failed";
    public const string ValidationFailedMessage = "One or more validation errors occurred.";

    /// <summary>Field name to messages, only set for <see cref="ErrorType.Validation"/>.</summary>
    public IReadOnlyDictionary<string, string[]>? ValidationErrors { get; init; }

    public static Error Validation(IReadOnlyDictionary<string, string[]> errors) =>
        new(ValidationFailedCode, ValidationFailedMessage, ErrorType.Validation) { ValidationErrors = errors };

    public static Error Validation(string field, params string[] messages) =>
        Validation(new Dictionary<string, string[]> { [field] = messages });
}
