namespace AiSdlc.Domain.Projects;

/// <summary>
/// One uploaded version of a project input. Versions are never overwritten: a new upload is a new version.
/// Deleting removes the stored content but keeps this row so the history and audit trail stay intact.
/// </summary>
public sealed class ProjectFile
{
    public Guid Id { get; init; }

    public Guid ProjectId { get; init; }

    public FileKind Kind { get; init; }

    public int Version { get; init; }

    /// <summary>The name as uploaded, already sanitised. Never used to build a path.</summary>
    public required string FileName { get; init; }

    public long SizeBytes { get; init; }

    /// <summary>Hex SHA-256 of the content, for integrity checks and duplicate detection.</summary>
    public required string Sha256 { get; init; }

    /// <summary>Server-generated key in file storage.</summary>
    public required string StorageKey { get; init; }

    public Guid UploadedById { get; init; }

    public DateTimeOffset UploadedAtUtc { get; init; }

    public DateTimeOffset? DeletedAtUtc { get; private set; }

    public Guid? DeletedById { get; private set; }

    public bool IsDeleted => DeletedAtUtc is not null;

    public void MarkDeleted(Guid deletedBy, DateTimeOffset now)
    {
        DeletedAtUtc ??= now;
        DeletedById ??= deletedBy;
    }
}
