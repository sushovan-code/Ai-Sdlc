namespace AiSdlc.Application.Wrapup;

/// <summary>One requirement and everything downstream that traces back to it.</summary>
public sealed record TraceRowDto(
    string RequirementId,
    string Title,
    IReadOnlyList<TraceLinkDto> Architecture,
    IReadOnlyList<TraceLinkDto> Tasks,
    IReadOnlyList<TraceLinkDto> TestCases);
