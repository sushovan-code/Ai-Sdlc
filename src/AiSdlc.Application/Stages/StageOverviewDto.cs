namespace AiSdlc.Application.Stages;

/// <summary>A stage page: who reviews it, whether a draft can be made now, and every version, newest first.</summary>
public sealed record StageOverviewDto(
    string ApproverRole,
    bool CanGenerate,
    string? BlockedReason,
    IReadOnlyList<StageVersionDto> Versions);
