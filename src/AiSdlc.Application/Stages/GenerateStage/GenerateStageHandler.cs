using AiSdlc.Application.Abstractions;
using AiSdlc.Application.Analysis;
using AiSdlc.Application.Analysis.Agents;
using AiSdlc.Application.Common;
using AiSdlc.Application.Projects;
using AiSdlc.Application.Stages.Agents;
using AiSdlc.Domain.Artifacts;
using AiSdlc.Domain.Auditing;
using FluentValidation;

namespace AiSdlc.Application.Stages.GenerateStage;

/// <summary>
/// Drafts the architecture, tasks or test cases from the earlier approved output, stores the result as a new version
/// waiting for review and audits it. Only the stage reviewer can do this, and only while the stage is open.
/// </summary>
public sealed class GenerateStageHandler(
    IValidator<GenerateStage> validator,
    ICurrentUser currentUser,
    IProjectRepository projects,
    StageGenerator generator,
    AnalysisGenerationLock generationLock,
    IAuditLog audit,
    TimeProvider time)
{
    public async Task<Result<StageVersionDto>> HandleAsync(GenerateStage command, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<StageVersionDto>.Failure(validation.ToError());
        }

        var opened = await ProjectAccess.OpenForRoleAsync(
            projects, currentUser, command.ProjectId, StageRules.ApproverRole(command.Stage), cancellationToken);
        if (opened.IsFailure)
        {
            return Result<StageVersionDto>.Failure(opened.Error);
        }

        var project = opened.Value;
        if (!project.CanDraft(command.Stage))
        {
            return Result<StageVersionDto>.Failure(AnalysisErrors.NotAvailable(StageText.BlockedReason(project, command.Stage)));
        }

        using var slot = generationLock.TryEnter(project.Id);
        if (slot is null)
        {
            return Result<StageVersionDto>.Failure(AnalysisErrors.GenerationInProgress);
        }

        var source = StageSourceBuilder.Build(project, command.Stage);
        if (source.IsFailure)
        {
            return Result<StageVersionDto>.Failure(source.Error);
        }

        var latest = project.LatestArtifact(command.Stage);
        var previous = latest is null ? null : StageMapper.ReadContent(latest);
        var feedback = string.IsNullOrWhiteSpace(command.Feedback) ? null : command.Feedback.Trim();

        var draft = await generator.GenerateAsync(project.Id, command.Stage, source.Value, previous, feedback, cancellationToken);
        if (draft.IsFailure)
        {
            return Result<StageVersionDto>.Failure(draft.Error);
        }

        var version = project.AddDraft(
            command.Stage,
            AnalysisJson.Serialize(draft.Value.Content),
            AnalysisJson.Serialize(draft.Value.Notes),
            ArtifactSource.Ai,
            draft.Value.Model,
            StagePrompts.Version,
            feedback,
            currentUser.UserId!.Value,
            time.GetUtcNow());
        await projects.SaveChangesAsync(cancellationToken);

        await audit.WriteAsync(
            new AuditRecord(
                AuditActions.StageGenerated,
                AuditOutcome.Success,
                currentUser.UserId,
                currentUser.Email,
                StageText.Details(project, command.Stage, version.Version, feedback is null ? null : "(with feedback)")),
            cancellationToken);

        return Result<StageVersionDto>.Success(StageMapper.ToDto(version));
    }
}
