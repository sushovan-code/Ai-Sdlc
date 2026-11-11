using AiSdlc.Application.Abstractions;
using AiSdlc.Application.Analysis;
using AiSdlc.Application.Common;
using AiSdlc.Application.Projects;
using AiSdlc.Domain.Auditing;
using AiSdlc.Domain.Projects;

namespace AiSdlc.Application.Files.DeleteProjectFile;

/// <summary>Removes the stored content of one file version. The metadata row stays so history and audit remain complete.</summary>
public sealed class DeleteProjectFileHandler(
    ICurrentUser currentUser,
    IProjectRepository projects,
    IFileStorage storage,
    IAuditLog audit,
    TimeProvider time)
{
    public async Task<Result> HandleAsync(DeleteProjectFile command, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Result.Failure(AuthErrors.InvalidCredentials);
        }

        var opened = await ProjectAccess.OpenAsync(projects, currentUser, command.ProjectId, true, cancellationToken);
        if (opened.IsFailure)
        {
            return Result.Failure(opened.Error);
        }

        var target = opened.Value.Files.FirstOrDefault(f => f.Id == command.FileId && !f.IsDeleted);
        if (target is { Kind: FileKind.Requirements } && opened.Value.RequirementsLocked)
        {
            return Result.Failure(AnalysisErrors.InputsLocked);
        }

        var file = opened.Value.DeleteFile(command.FileId, userId, time.GetUtcNow());
        if (file is null)
        {
            return Result.Failure(ProjectErrors.FileNotFound);
        }

        await projects.SaveChangesAsync(cancellationToken);
        await storage.DeleteAsync(file.StorageKey, cancellationToken);
        await audit.WriteAsync(
            new AuditRecord(
                AuditActions.FileDeleted,
                AuditOutcome.Success,
                userId,
                currentUser.Email,
                $"{file.ProjectId}: {file.Kind} v{file.Version} {file.FileName}"),
            cancellationToken);
        return Result.Success();
    }
}
