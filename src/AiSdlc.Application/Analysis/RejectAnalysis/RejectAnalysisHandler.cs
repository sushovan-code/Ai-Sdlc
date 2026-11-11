using AiSdlc.Application.Abstractions;
using AiSdlc.Application.Common;
using AiSdlc.Application.Projects;
using AiSdlc.Domain.Artifacts;
using AiSdlc.Domain.Auditing;
using FluentValidation;

namespace AiSdlc.Application.Analysis.RejectAnalysis;

/// <summary>Gate 1: the Project Manager rejects the newest draft with a reason; the project stays at this gate.</summary>
public sealed class RejectAnalysisHandler(
    IValidator<RejectAnalysis> validator,
    ICurrentUser currentUser,
    IProjectRepository projects,
    AnalysisGenerationLock generationLock,
    IAuditLog audit,
    TimeProvider time)
{
    public async Task<Result> HandleAsync(RejectAnalysis command, CancellationToken cancellationToken)
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

        var outcome = project.RejectAnalysis(
            command.Version, currentUser.UserId!.Value, currentUser.Role!, command.Comment.Trim(), time.GetUtcNow());
        if (outcome != GateOutcome.Done)
        {
            return Result.Failure(AnalysisErrors.FromOutcome(outcome));
        }

        await projects.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync(
            new AuditRecord(
                AuditActions.AnalysisRejected,
                AuditOutcome.Success,
                currentUser.UserId,
                currentUser.Email,
                $"{project.Id}: analysis v{command.Version}"),
            cancellationToken);
        return Result.Success();
    }
}
