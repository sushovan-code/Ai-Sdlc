namespace AiSdlc.Domain.Artifacts;

/// <summary>The AI-drafted outputs of the lifecycle. Each is reviewed at a human gate before the next stage opens.</summary>
public enum ArtifactStage
{
    /// <summary>Requirements analysis, reviewed at gate 1 by the Project Manager.</summary>
    Analysis,

    /// <summary>Architecture plan, reviewed at gate 2 by the Software Architect.</summary>
    Architecture,

    /// <summary>Development tasks and their sprint assignments, reviewed at gate 3 by the Project Manager.</summary>
    Tasks,

    /// <summary>QA test cases, reviewed at gate 4 (the final gate) by QA.</summary>
    TestCases,
}
