using AiSdlc.Application.Abstractions;
using AiSdlc.Application.Analysis;
using AiSdlc.Application.Common;
using AiSdlc.Application.Projects;
using AiSdlc.Domain.Artifacts;
using AiSdlc.Domain.Auditing;
using AiSdlc.Domain.Projects;

namespace AiSdlc.Application.Stages.DecideStage;

/// <summary>
/// Approves or rejects the newest version of a stage at its gate. Only the stage reviewer can, and not while a draft is
/// being generated. Each decision records reviewer, role, time and comment, and is audited.
/// </summary>
public sealed class DecideStageHandler(
    ICurrentUser currentUser,
    IProjectRepository projects,
    AnalysisGenerationLock generationLock,
    IAuditLog audit,
    TimeProvider time)
{
    public Task<Result> ApproveAsync(DecideStage command, CancellationToken cancellationToken) =>
        DecideAsync(command, approve: true, cancellationToken);

    public Task<Result> RejectAsync(DecideStage command, CancellationToken cancellationToken) =>
        DecideAsync(command, approve: false, cancellationToken);

    private async Task<Result> DecideAsync(DecideStage command, bool approve, CancellationToken cancellationToken)
    {
        var comment = string.IsNullOrWhiteSpace(command.Comment) ? null : command.Comment.Trim();
        if (comment?.Length > AiLimits.MaxFeedbackLength)
        {
            return Result.Failure(Error.Validation("comment", "The comment is too long."));
        }

        if (!approve && comment is null)
        {
            return Result.Failure(Error.Validation("comment", "A reason is needed to reject."));
        }

        var opened = await ProjectAccess.OpenForRoleAsync(
            projects, currentUser, command.ProjectId, StageRules.ApproverRole(command.Stage), cancellationToken);
        if (opened.IsFailure)
        {
            return Result.Failure(opened.Error);
        }

        var project = opened.Value;
        if (generationLock.IsRunning(project.Id))
        {
            return Result.Failure(AnalysisErrors.GenerationInProgress);
        }

        var outcome = Decide(project, command, approve, comment);
        if (outcome != GateOutcome.Done)
        {
            return Result.Failure(AnalysisErrors.FromOutcome(outcome));
        }

        await projects.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync(
            new AuditRecord(
                approve ? AuditActions.StageApproved : AuditActions.StageRejected,
                AuditOutcome.Success,
                currentUser.UserId,
                currentUser.Email,
                StageText.Details(project, command.Stage, command.Version)),
            cancellationToken);
        return Result.Success();
    }

    private GateOutcome Decide(Project project, DecideStage command, bool approve, string? comment)
    {
        var reviewer = currentUser.UserId!.Value;
        var role = currentUser.Role!;
        var now = time.GetUtcNow();
        return approve
            ? project.Approve(command.Stage, command.Version, reviewer, role, comment, now)
            : project.Reject(command.Stage, command.Version, reviewer, role, comment!, now);
    }
}
