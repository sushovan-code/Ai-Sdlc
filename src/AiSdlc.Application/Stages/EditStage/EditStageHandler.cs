using AiSdlc.Application.Abstractions;
using AiSdlc.Application.Analysis;
using AiSdlc.Application.Analysis.Agents;
using AiSdlc.Application.Common;
using AiSdlc.Application.Projects;
using AiSdlc.Domain.Artifacts;
using AiSdlc.Domain.Auditing;

namespace AiSdlc.Application.Stages.EditStage;

/// <summary>
/// Lets the stage reviewer correct a draft by hand. The corrected text is saved as a new version marked as written by a
/// person, waiting for review like any other, so the AI draft and every edit stay in the history. Analysis is reviewed
/// but not edited by hand.
/// </summary>
public sealed class EditStageHandler(
    ICurrentUser currentUser,
    IProjectRepository projects,
    AnalysisGenerationLock generationLock,
    IAuditLog audit,
    TimeProvider time)
{
    private const string ManualModel = "human";
    private const string ManualPrompt = "manual";

    public async Task<Result<StageVersionDto>> HandleAsync(EditStage command, CancellationToken cancellationToken)
    {
        if (command.Stage == ArtifactStage.Analysis)
        {
            return Result<StageVersionDto>.Failure(AnalysisErrors.NotAvailable("The analysis is reviewed, not edited by hand."));
        }

        var content = StageContentRules.Normalize(command.Content);
        if (StageContentRules.Problem(content) is { } problem)
        {
            return Result<StageVersionDto>.Failure(Error.Validation("content", problem));
        }

        var opened = await ProjectAccess.OpenForRoleAsync(
            projects, currentUser, command.ProjectId, StageRules.ApproverRole(command.Stage), cancellationToken);
        if (opened.IsFailure)
        {
            return Result<StageVersionDto>.Failure(opened.Error);
        }

        var project = opened.Value;
        var latest = project.LatestArtifact(command.Stage);
        if (!project.CanDraft(command.Stage) || latest is null)
        {
            return Result<StageVersionDto>.Failure(AnalysisErrors.NotAvailable("There is no open draft to edit."));
        }

        if (generationLock.IsRunning(project.Id))
        {
            return Result<StageVersionDto>.Failure(AnalysisErrors.GenerationInProgress);
        }

        var version = project.AddDraft(
            command.Stage,
            AnalysisJson.Serialize(content),
            latest.NotesJson,
            ArtifactSource.Human,
            ManualModel,
            ManualPrompt,
            null,
            currentUser.UserId!.Value,
            time.GetUtcNow());
        await projects.SaveChangesAsync(cancellationToken);

        await audit.WriteAsync(
            new AuditRecord(
                AuditActions.StageEdited,
                AuditOutcome.Success,
                currentUser.UserId,
                currentUser.Email,
                StageText.Details(project, command.Stage, version.Version)),
            cancellationToken);
        return Result<StageVersionDto>.Success(StageMapper.ToDto(version));
    }
}
