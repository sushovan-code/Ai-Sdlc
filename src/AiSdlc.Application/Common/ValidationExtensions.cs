using FluentValidation.Results;

namespace AiSdlc.Application.Common;

internal static class ValidationExtensions
{
    public static Error ToError(this ValidationResult validation) =>
        Error.Validation(new Dictionary<string, string[]>(validation.ToDictionary()));
}
