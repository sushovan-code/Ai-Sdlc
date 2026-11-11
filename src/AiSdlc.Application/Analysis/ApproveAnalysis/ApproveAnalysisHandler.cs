using AiSdlc.Application.Abstractions;
using AiSdlc.Application.Common;
using AiSdlc.Application.Projects;
using AiSdlc.Domain.Artifacts;
using AiSdlc.Domain.Auditing;
using FluentValidation;

namespace AiSdlc.Application.Analysis.ApproveAnalysis;

/// <summary>Gate 1: the Project Manager approves the newest draft, which unlocks architecture.</summary>
public sealed class ApproveAnalysisHandler(
    IValidator<ApproveAnalysis> validator,
    ICurrentUser currentUser,
    IProjectRepository projects,
    AnalysisGenerationLock generationLock,
    IAuditLog audit,
    TimeProvider time)
{
    public async Task<Result> HandleAsync(ApproveAnalysis command, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Result.Failure(validation.ToError());
        }

        var opened = await ProjectAccess.OpenAsync(projects, currentUser, command.ProjectId, true, cancellationToken);
        if (opened.IsFailure)
        {
            return Result.Failure(opened.Error);
        }

        var project = opened.Value;
        if (generationLock.IsRunning(project.Id))
        {
            return Result.Failure(AnalysisErrors.GenerationInProgress);
        }

        var comment = string.IsNullOrWhiteSpace(command.Comment) ? null : command.Comment.Trim();
        var outcome = project.ApproveAnalysis(
            command.Version, currentUser.UserId!.Value, currentUser.Role!, comment, time.GetUtcNow());
        if (outcome != GateOutcome.Done)
        {
            return Result.Failure(AnalysisErrors.FromOutcome(outcome));
        }

        await projects.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync(
            new AuditRecord(
                AuditActions.AnalysisApproved,
                AuditOutcome.Success,
                currentUser.UserId,
                currentUser.Email,
                $"{project.Id}: analysis v{command.Version}"),
            cancellationToken);
        return Result.Success();
    }
}
