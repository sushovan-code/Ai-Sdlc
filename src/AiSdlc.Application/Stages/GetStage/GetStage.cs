using AiSdlc.Domain.Artifacts;

namespace AiSdlc.Application.Stages.GetStage;

public sealed record GetStage(Guid ProjectId, ArtifactStage Stage);
