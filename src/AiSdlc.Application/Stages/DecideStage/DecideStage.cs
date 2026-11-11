using AiSdlc.Domain.Artifacts;

namespace AiSdlc.Application.Stages.DecideStage;

/// <summary>Approve or reject one version. The comment is optional to approve and required to reject.</summary>
public sealed record DecideStage(Guid ProjectId, ArtifactStage Stage, int Version, string? Comment);
