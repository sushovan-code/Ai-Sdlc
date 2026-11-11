using AiSdlc.Domain.Artifacts;

namespace AiSdlc.Application.Stages.GenerateStage;

/// <summary>Drafts a stage. Feedback is the reviewer comment when regenerating.</summary>
public sealed record GenerateStage(Guid ProjectId, ArtifactStage Stage, string? Feedback);
