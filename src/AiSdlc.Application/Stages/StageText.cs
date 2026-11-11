using AiSdlc.Domain.Artifacts;
using AiSdlc.Domain.Projects;

namespace AiSdlc.Application.Stages;

internal static class StageText
{
    /// <summary>Why a stage cannot be drafted right now, in words for the user.</summary>
    public static string BlockedReason(Project project, ArtifactStage stage)
    {
        var required = StageRules.RequiredProjectStage(stage);
        if (project.Stage > required)
        {
            return "This stage has already been approved.";
        }

        return stage == ArtifactStage.Analysis
            ? "Upload the requirements and the team sheet first."
            : $"Gate {StageRules.GateNumber(stage - 1)} must be approved first.";
    }

    public static string Details(Project project, ArtifactStage stage, int version, string? extra = null) =>
        $"{project.Id}: {stage} v{version}{(extra is null ? string.Empty : " " + extra)}";
}
