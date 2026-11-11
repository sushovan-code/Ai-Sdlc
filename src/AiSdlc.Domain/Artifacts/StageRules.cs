using AiSdlc.Domain.Projects;
using AiSdlc.Domain.Security;

namespace AiSdlc.Domain.Artifacts;

/// <summary>The lifecycle order: where a project must be to draft a stage, where approval leaves it, and who approves.</summary>
public static class StageRules
{
    /// <summary>The project stage a stage's output is drafted in (the stage just before its gate).</summary>
    public static ProjectStage RequiredProjectStage(ArtifactStage stage) => stage switch
    {
        ArtifactStage.Analysis => ProjectStage.FilesUploaded,
        ArtifactStage.Architecture => ProjectStage.AnalysisApproved,
        ArtifactStage.Tasks => ProjectStage.ArchitectureApproved,
        ArtifactStage.TestCases => ProjectStage.TasksApproved,
        _ => throw new ArgumentOutOfRangeException(nameof(stage)),
    };

    /// <summary>The project stage after the stage's gate is passed.</summary>
    public static ProjectStage ApprovedProjectStage(ArtifactStage stage) => stage switch
    {
        ArtifactStage.Analysis => ProjectStage.AnalysisApproved,
        ArtifactStage.Architecture => ProjectStage.ArchitectureApproved,
        ArtifactStage.Tasks => ProjectStage.TasksApproved,
        ArtifactStage.TestCases => ProjectStage.TestCasesApproved,
        _ => throw new ArgumentOutOfRangeException(nameof(stage)),
    };

    /// <summary>The role that generates, edits and decides the stage. The administrator never approves a gate.</summary>
    public static string ApproverRole(ArtifactStage stage) => stage switch
    {
        ArtifactStage.Analysis => AppRoles.ProjectManager,
        ArtifactStage.Architecture => AppRoles.Architect,
        ArtifactStage.Tasks => AppRoles.ProjectManager,
        ArtifactStage.TestCases => AppRoles.QA,
        _ => throw new ArgumentOutOfRangeException(nameof(stage)),
    };

    /// <summary>The gate that closes the stage, numbered 1 to 4.</summary>
    public static int GateNumber(ArtifactStage stage) => (int)stage + 1;
}
