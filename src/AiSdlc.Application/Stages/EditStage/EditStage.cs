using AiSdlc.Application.Stages.Content;
using AiSdlc.Domain.Artifacts;

namespace AiSdlc.Application.Stages.EditStage;

/// <summary>A persons corrected version of a stage. It becomes a new version waiting for review.</summary>
public sealed record EditStage(Guid ProjectId, ArtifactStage Stage, StageContent Content);
