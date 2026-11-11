namespace AiSdlc.Domain.Artifacts;

/// <summary>The AI-drafted outputs of the lifecycle. Each is reviewed at a human gate before the next stage opens.</summary>
public enum ArtifactStage
{
    /// <summary>Requirements analysis, reviewed at gate 1 by the Project Manager.</summary>
    Analysis,
}
