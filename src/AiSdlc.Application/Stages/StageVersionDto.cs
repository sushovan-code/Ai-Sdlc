using AiSdlc.Application.Analysis;
using AiSdlc.Application.Stages.Content;
using AiSdlc.Domain.Artifacts;

namespace AiSdlc.Application.Stages;

/// <summary>One draft of a stage with its notes and review history.</summary>
public sealed record StageVersionDto(
    int Version,
    ArtifactStatus Status,
    ArtifactSource Source,
    string Model,
    string PromptVersion,
    string? Feedback,
    int? BasedOnVersion,
    DateTimeOffset CreatedAtUtc,
    StageContent Content,
    StageNotes Notes,
    IReadOnlyList<ReviewDecisionDto> Decisions);
