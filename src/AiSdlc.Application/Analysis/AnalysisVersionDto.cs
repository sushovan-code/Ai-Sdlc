using AiSdlc.Application.Analysis.Content;
using AiSdlc.Domain.Artifacts;

namespace AiSdlc.Application.Analysis;

/// <summary>One draft of the analysis with the notes and review history that belong to it.</summary>
public sealed record AnalysisVersionDto(
    int Version,
    ArtifactStatus Status,
    ArtifactSource Source,
    string Model,
    string PromptVersion,
    string? Feedback,
    int? BasedOnVersion,
    DateTimeOffset CreatedAtUtc,
    AnalysisContent Content,
    AnalysisNotes Notes,
    IReadOnlyList<ReviewDecisionDto> Decisions);
