using System.Security.Cryptography;
using AiSdlc.Application.Abstractions;
using AiSdlc.Application.Common;
using AiSdlc.Application.Projects;
using AiSdlc.Application.Team.Import;
using AiSdlc.Domain.Auditing;
using AiSdlc.Domain.Projects;

namespace AiSdlc.Application.Files.UploadProjectFile;

/// <summary>
/// Accepts a new version of a project input. The file is checked (type, signature, structure, size), hashed and stored
/// under a server-generated key. Rejections are audited too.
/// </summary>
public sealed class UploadProjectFileHandler(
    ICurrentUser currentUser,
    IProjectRepository projects,
    IUserAccounts accounts,
    IFileStorage storage,
    ITeamSheetReader teamSheets,
    IAuditLog audit,
    TimeProvider time)
{
    public async Task<Result<ProjectFileDto>> HandleAsync(UploadProjectFile command, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Result<ProjectFileDto>.Failure(AuthErrors.InvalidCredentials);
        }

        var opened = await ProjectAccess.OpenAsync(projects, currentUser, command.ProjectId, true, cancellationToken);
        if (opened.IsFailure)
        {
            return Result<ProjectFileDto>.Failure(opened.Error);
        }

        var project = opened.Value;
        var fileName = FileNames.Sanitize(command.FileName);
        var extension = FileNames.ExtensionOf(fileName);

        var content = await ReadBoundedAsync(command.Content, cancellationToken);
        var rejection = Check(command, extension, content);
        IReadOnlyList<TeamMemberDraft> team = [];
        if (rejection is null && command.Kind == FileKind.TeamTemplate)
        {
            var parsed = ReadTeam(content);
            rejection = parsed.IsFailure ? parsed.Error : null;
            team = parsed.IsSuccess ? parsed.Value : [];
        }

        if (rejection is not null)
        {
            await AuditRejectionAsync(project.Id, fileName, rejection, cancellationToken);
            return Result<ProjectFileDto>.Failure(rejection);
        }

        var storageKey = $"projects/{project.Id:N}/{Guid.NewGuid():N}";
        await using (var stream = new MemoryStream(content, writable: false))
        {
            await storage.SaveAsync(storageKey, stream, cancellationToken);
        }

        var file = project.AddFile(
            command.Kind,
            fileName,
            content.Length,
            Convert.ToHexString(SHA256.HashData(content)),
            storageKey,
            userId,
            time.GetUtcNow());

        if (command.Kind == FileKind.TeamTemplate)
        {
            project.AttachTeam(file, team);
        }

        await SaveOrCleanUpAsync(storageKey, cancellationToken);

        await audit.WriteAsync(
            new AuditRecord(
                AuditActions.FileUploaded,
                AuditOutcome.Success,
                currentUser.UserId,
                currentUser.Email,
                $"{project.Id}: {command.Kind} v{file.Version} {file.FileName}"),
            cancellationToken);

        var uploader = await accounts.FindAsync(file.UploadedById, cancellationToken);
        return Result<ProjectFileDto>.Success(new ProjectFileDto(
            file.Id,
            file.Kind,
            file.Version,
            file.FileName,
            file.SizeBytes,
            file.Sha256,
            file.UploadedAtUtc,
            file.UploadedById,
            uploader?.DisplayName ?? string.Empty,
            IsLatest: true));
    }

    /// <summary>The people on a team sheet. A sheet with any bad row is refused as a whole, with every problem listed.</summary>
    private Result<IReadOnlyList<TeamMemberDraft>> ReadTeam(byte[] content)
    {
        var rows = teamSheets.Read(content);
        return rows.IsFailure
            ? Result<IReadOnlyList<TeamMemberDraft>>.Failure(rows.Error)
            : TeamSheetValidator.Validate(rows.Value);
    }

    private static Error? Check(UploadProjectFile command, string extension, byte[] content)
    {
        if (content.Length > FileRules.MaxFileBytes)
        {
            return Error.Validation("file", $"The file is larger than {FileRules.MaxFileBytes / (1024 * 1024)} MB.");
        }

        if (!FileRules.IsAllowed(command.Kind, extension))
        {
            var allowed = string.Join(", ", FileRules.ExtensionsFor(command.Kind));
            return Error.Validation("file", $"Only {allowed} files are accepted here.");
        }

        var inspected = FileContentInspector.Inspect(command.Kind, extension, content);
        return inspected.IsFailure ? inspected.Error : null;
    }

    /// <summary>Reads up to the limit plus one byte, so an oversized upload is detected without buffering all of it.</summary>
    private static async Task<byte[]> ReadBoundedAsync(Stream source, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        var limit = FileRules.MaxFileBytes + 1;
        var chunk = new byte[81920];
        int read;
        while (buffer.Length < limit
               && (read = await source.ReadAsync(chunk.AsMemory(0, (int)Math.Min(chunk.Length, limit - buffer.Length)), cancellationToken)) > 0)
        {
            buffer.Write(chunk, 0, read);
        }

        return buffer.ToArray();
    }

    /// <summary>If saving the row fails, remove the orphaned content so storage and database stay consistent.</summary>
    private async Task SaveOrCleanUpAsync(string storageKey, CancellationToken cancellationToken)
    {
        try
        {
            await projects.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await storage.DeleteAsync(storageKey, CancellationToken.None);
            throw;
        }
    }

    private Task AuditRejectionAsync(Guid projectId, string fileName, Error error, CancellationToken cancellationToken) =>
        audit.WriteAsync(
            new AuditRecord(
                AuditActions.FileRejected,
                AuditOutcome.Failure,
                currentUser.UserId,
                currentUser.Email,
                $"{projectId}: {fileName} ({error.Code})"),
            cancellationToken);
}
