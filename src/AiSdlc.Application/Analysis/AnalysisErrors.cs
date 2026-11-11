using AiSdlc.Application.Common;

namespace AiSdlc.Application.Analysis;

public static class AnalysisErrors
{
    public const string NotAvailableCode = "analysis.not-available";
    public const string RequirementsUnreadableCode = "analysis.requirements-unreadable";
    public const string TooLongCode = "analysis.requirements-too-long";
    public const string VersionNotFoundCode = "analysis.version-not-found";
    public const string NotLatestCode = "analysis.not-latest";
    public const string NotPendingCode = "analysis.not-pending";
    public const string AiUnavailableCode = "ai.unavailable";
    public const string AiInvalidOutputCode = "ai.invalid-output";
    public const string InputsLockedCode = "project.inputs-locked";

    public static Error AiUnavailable { get; } = new(
        AiUnavailableCode,
        "The AI model could not be reached or did not answer. Check the AI settings and try again.",
        ErrorType.Unavailable);

    public static Error AiInvalidOutput { get; } = new(
        AiInvalidOutputCode,
        "The AI model returned an answer that could not be used, even after a retry. Try again, or add feedback to steer it.",
        ErrorType.Unavailable);

    public static Error VersionNotFound { get; } =
        new(VersionNotFoundCode, "That version of the analysis does not exist.", ErrorType.NotFound);

    public static Error NotLatest { get; } =
        new(NotLatestCode, "Only the newest version of the analysis can be decided.", ErrorType.Conflict);

    public static Error NotPending { get; } =
        new(NotPendingCode, "That version has already been decided or replaced.", ErrorType.Conflict);

    public static Error InputsLocked { get; } = new(
        InputsLockedCode,
        "The requirements are locked because the analysis has been approved.",
        ErrorType.Conflict);

    public static Error NotAvailable(string reason) => new(NotAvailableCode, reason, ErrorType.Conflict);
}
