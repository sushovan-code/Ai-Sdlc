using AiSdlc.Application.Abstractions;
using AiSdlc.Application.Common;
using AiSdlc.Application.Projects;
using AiSdlc.Domain.Auditing;

namespace AiSdlc.Application.Files.DownloadProjectFile;

/// <summary>Any member of the project may download its files; everyone else gets "not found".</summary>
public sealed class DownloadProjectFileHandler(
    ICurrentUser currentUser, IProjectRepository projects, IFileStorage storage, IAuditLog audit)
{
    public async Task<Result<FileDownload>> HandleAsync(DownloadProjectFile query, CancellationToken cancellationToken)
    {
        var opened = await ProjectAccess.OpenAsync(projects, currentUser, query.ProjectId, false, cancellationToken);
        if (opened.IsFailure)
        {
            return Result<FileDownload>.Failure(opened.Error);
        }

        var file = opened.Value.Files.FirstOrDefault(f => f.Id == query.FileId && !f.IsDeleted);
        if (file is null)
        {
            return Result<FileDownload>.Failure(ProjectErrors.FileNotFound);
        }

        var content = await storage.OpenReadAsync(file.StorageKey, cancellationToken);
        if (content is null)
        {
            return Result<FileDownload>.Failure(ProjectErrors.FileNotFound);
        }

        await audit.WriteAsync(
            new AuditRecord(
                AuditActions.FileDownloaded,
                AuditOutcome.Success,
                currentUser.UserId,
                currentUser.Email,
                $"{file.ProjectId}: {file.Kind} v{file.Version}"),
            cancellationToken);

        var contentType = FileRules.ContentTypeFor(FileNames.ExtensionOf(file.FileName));
        return Result<FileDownload>.Success(new FileDownload(content, file.FileName, contentType));
    }
}
