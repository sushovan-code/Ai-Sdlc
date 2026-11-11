namespace AiSdlc.Domain.Projects;

/// <summary>A software project that moves from requirements to approved test cases. The aggregate guards its own invariants.</summary>
public sealed class Project
{
    private readonly List<ProjectMember> _members = [];
    private readonly List<ProjectFile> _files = [];

    private Project()
    {
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public Guid CreatedById { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public ProjectStage Stage { get; private set; }

    public IReadOnlyCollection<ProjectMember> Members => _members;

    public IReadOnlyCollection<ProjectFile> Files => _files;

    /// <summary>Creates a project whose creator is its first member and owner.</summary>
    public static Project Create(string name, string? description, Guid creatorId, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            CreatedById = creatorId,
            CreatedAtUtc = now,
            Stage = ProjectStage.Draft,
        };
        project.AddMember(creatorId, now);
        return project;
    }

    public bool IsMember(Guid userId) => _members.Exists(m => m.UserId == userId);

    /// <returns><c>false</c> when the user already belongs to the project.</returns>
    public bool AddMember(Guid userId, DateTimeOffset now)
    {
        if (IsMember(userId))
        {
            return false;
        }

        _members.Add(new ProjectMember { ProjectId = Id, UserId = userId, AddedAtUtc = now });
        return true;
    }

    public MemberRemoval RemoveMember(Guid userId)
    {
        if (userId == CreatedById)
        {
            return MemberRemoval.IsOwner;
        }

        var member = _members.Find(m => m.UserId == userId);
        if (member is null)
        {
            return MemberRemoval.NotAMember;
        }

        _members.Remove(member);
        return MemberRemoval.Removed;
    }

    /// <summary>Adds a new version of <paramref name="kind"/>. Version numbers keep counting even after deletions.</summary>
    public ProjectFile AddFile(
        FileKind kind,
        string fileName,
        long sizeBytes,
        string sha256,
        string storageKey,
        Guid uploadedBy,
        DateTimeOffset now)
    {
        var nextVersion = _files.Where(f => f.Kind == kind).Select(f => f.Version).DefaultIfEmpty(0).Max() + 1;
        var file = new ProjectFile
        {
            Id = Guid.NewGuid(),
            ProjectId = Id,
            Kind = kind,
            Version = nextVersion,
            FileName = fileName,
            SizeBytes = sizeBytes,
            Sha256 = sha256,
            StorageKey = storageKey,
            UploadedById = uploadedBy,
            UploadedAtUtc = now,
        };

        _files.Add(file);
        RefreshStage();
        return file;
    }

    /// <returns>The deleted file, or <c>null</c> when it does not exist or was already deleted.</returns>
    public ProjectFile? DeleteFile(Guid fileId, Guid deletedBy, DateTimeOffset now)
    {
        var file = _files.Find(f => f.Id == fileId && !f.IsDeleted);
        if (file is null)
        {
            return null;
        }

        file.MarkDeleted(deletedBy, now);
        RefreshStage();
        return file;
    }

    /// <summary>The newest file of a kind that has not been deleted.</summary>
    public ProjectFile? LatestFile(FileKind kind) =>
        _files.Where(f => f.Kind == kind && !f.IsDeleted).OrderByDescending(f => f.Version).FirstOrDefault();

    private void RefreshStage()
    {
        var hasBoth = LatestFile(FileKind.Requirements) is not null && LatestFile(FileKind.TeamTemplate) is not null;
        Stage = hasBoth ? ProjectStage.FilesUploaded : ProjectStage.Draft;
    }
}
