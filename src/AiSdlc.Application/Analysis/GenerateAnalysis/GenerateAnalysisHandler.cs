using AiSdlc.Application.Abstractions;
using AiSdlc.Application.Analysis.Agents;
using AiSdlc.Application.Analysis.Content;
using AiSdlc.Application.Common;
using AiSdlc.Application.Files;
using AiSdlc.Application.Projects;
using AiSdlc.Domain.Artifacts;
using AiSdlc.Domain.Auditing;
using AiSdlc.Domain.Projects;
using FluentValidation;

namespace AiSdlc.Application.Analysis.GenerateAnalysis;

/// <summary>
/// Gate 1 input: reads the latest requirements, runs the agents and stores the result as a new version waiting for
/// review. With feedback, the previous version and the comment steer the next draft. Project Managers only.
/// </summary>
public sealed class GenerateAnalysisHandler(
    IValidator<GenerateAnalysis> validator,
    ICurrentUser currentUser,
    IProjectRepository projects,
    IFileStorage storage,
    IRequirementsReader reader,
    AnalysisOrchestrator orchestrator,
    AnalysisGenerationLock generationLock,
    IAuditLog audit,
    TimeProvider time)
{
    public async Task<Result<AnalysisVersionDto>> HandleAsync(GenerateAnalysis command, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<AnalysisVersionDto>.Failure(validation.ToError());
        }

        var opened = await ProjectAccess.OpenAsync(projects, currentUser, command.ProjectId, true, cancellationToken);
        if (opened.IsFailure)
        {
            return Result<AnalysisVersionDto>.Failure(opened.Error);
        }

        var project = opened.Value;
        if (!project.CanDraftAnalysis)
        {
            return Result<AnalysisVersionDto>.Failure(AnalysisErrors.NotAvailable(BlockedReason(project)));
        }

        using var slot = generationLock.TryEnter(project.Id);
        if (slot is null)
        {
            return Result<AnalysisVersionDto>.Failure(AnalysisErrors.GenerationInProgress);
        }

        var text = await ReadRequirementsAsync(project, cancellationToken);
        if (text.IsFailure)
        {
            return Result<AnalysisVersionDto>.Failure(text.Error);
        }

        var latest = project.LatestArtifact(ArtifactStage.Analysis);
        var previous = latest is null ? null : AnalysisJson.Deserialize<AnalysisContent>(latest.ContentJson);
        var feedback = string.IsNullOrWhiteSpace(command.Feedback) ? null : command.Feedback.Trim();

        var run = await orchestrator.RunAsync(text.Value, previous, feedback, cancellationToken);
        if (run.IsFailure)
        {
            return Result<AnalysisVersionDto>.Failure(run.Error);
        }

        var draft = project.AddAnalysisDraft(
            AnalysisJson.Serialize(run.Value.Content),
            AnalysisJson.Serialize(run.Value.Notes),
            ArtifactSource.Ai,
            run.Value.Model,
            AnalysisPrompts.Version,
            feedback,
            currentUser.UserId!.Value,
            time.GetUtcNow());
        await projects.SaveChangesAsync(cancellationToken);

        await audit.WriteAsync(
            new AuditRecord(
                AuditActions.AnalysisGenerated,
                AuditOutcome.Success,
                currentUser.UserId,
                currentUser.Email,
                $"{project.Id}: analysis v{draft.Version}{(feedback is null ? string.Empty : " (with feedback)")}"),
            cancellationToken);

        return Result<AnalysisVersionDto>.Success(AnalysisMapper.ToDto(draft));
    }

    internal static string BlockedReason(Project project) => project.Stage == ProjectStage.Draft
        ? "Upload the requirements and the team sheet first."
        : "The analysis has already been approved.";

    private async Task<Result<string>> ReadRequirementsAsync(Project project, CancellationToken cancellationToken)
    {
        var file = project.LatestFile(FileKind.Requirements);
        if (file is null)
        {
            return Result<string>.Failure(AnalysisErrors.NotAvailable("Upload the requirements first."));
        }

        await using var stream = await storage.OpenReadAsync(file.StorageKey, cancellationToken);
        if (stream is null)
        {
            return Result<string>.Failure(ProjectErrors.FileNotFound);
        }

        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken);
        var text = reader.ReadText(buffer.ToArray(), FileNames.ExtensionOf(file.FileName));
        if (text.IsFailure)
        {
            return text;
        }

        return text.Value.Length > AiLimits.MaxSourceCharacters
            ? Result<string>.Failure(AnalysisErrors.RequirementsTooLong)
            : text;
    }
}
