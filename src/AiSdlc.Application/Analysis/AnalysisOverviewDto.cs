namespace AiSdlc.Application.Analysis;

/// <summary>The analysis page: whether a draft can be made now, and every version, newest first.</summary>
public sealed record AnalysisOverviewDto(
    bool CanGenerate,
    string? BlockedReason,
    IReadOnlyList<AnalysisVersionDto> Versions);
