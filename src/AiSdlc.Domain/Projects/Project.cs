using AiSdlc.Domain.Artifacts;
using AiSdlc.Domain.Planning;

namespace AiSdlc.Domain.Projects;

/// <summary>A software project that moves from requirements to approved test cases. The aggregate guards its own invariants.</summary>
public sealed class Project
{
    private readonly List<ProjectMember> _members = [];
    private readonly List<ProjectFile> _files = [];
    private readonly List<TeamMember> _team = [];
    private readonly List<ArtifactVersion> _artifacts = [];

    private Project()
    {
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public Guid CreatedById { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public ProjectStage Stage { get; private set; }

    /// <summary>Length of a sprint in weeks: 2, 3 or 4.</summary>
    public int SprintWeeks { get; private set; } = SprintRules.DefaultWeeks;

    public IReadOnlyCollection<ProjectMember> Members => _members;

    public IReadOnlyCollection<ProjectFile> Files => _files;

    /// <summary>Every team-sheet row ever imported, across versions. Use <see cref="CurrentTeam"/> to plan with.</summary>
    public IReadOnlyCollection<TeamMember> Team => _team;

    /// <summary>Every AI draft and its review history, across stages and versions.</summary>
    public IReadOnlyCollection<ArtifactVersion> Artifacts => _artifacts;

    /// <summary>Once the analysis is approved the requirements are frozen, so the approved analysis stays true to them.</summary>
    public bool RequirementsLocked => Stage >= ProjectStage.AnalysisApproved;

    /// <summary>The people on the newest team sheet that has not been deleted.</summary>
    public IReadOnlyList<TeamMember> CurrentTeam
    {
        get
        {
            var latest = LatestFile(FileKind.TeamTemplate);
            return latest is null ? [] : _team.Where(m => m.ProjectFileId == latest.Id).ToList();
        }
    }

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
        if (kind == FileKind.Requirements && RequirementsLocked)
        {
            throw new InvalidOperationException("The requirements cannot change after the analysis is approved.");
        }

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

        if (file.Kind == FileKind.Requirements && RequirementsLocked)
        {
            throw new InvalidOperationException("The requirements cannot change after the analysis is approved.");
        }

        file.MarkDeleted(deletedBy, now);
        RefreshStage();
        return file;
    }

    /// <returns><c>false</c> when <paramref name="weeks"/> is not an allowed sprint length.</returns>
    public bool SetSprintCadence(int weeks)
    {
        if (!SprintRules.IsValidCadence(weeks))
        {
            return false;
        }

        SprintWeeks = weeks;
        return true;
    }

    /// <summary>Stores the people from a validated team sheet against the file version they came from.</summary>
    public void AttachTeam(ProjectFile teamFile, IEnumerable<TeamMemberDraft> drafts)
    {
        ArgumentNullException.ThrowIfNull(teamFile);
        if (teamFile.ProjectId != Id || teamFile.Kind != FileKind.TeamTemplate)
        {
            throw new ArgumentException("The file must be a team template of this project.", nameof(teamFile));
        }

        _team.AddRange(drafts.Select(d => new TeamMember
        {
            Id = Guid.NewGuid(),
            ProjectId = Id,
            ProjectFileId = teamFile.Id,
            Name = d.Name,
            Role = d.Role,
            Skills = d.Skills,
            CapacityHours = d.CapacityHours,
            AvailabilityHours = d.AvailabilityHours,
        }));
    }

    /// <summary>The newest file of a kind that has not been deleted.</summary>
    public ProjectFile? LatestFile(FileKind kind) =>
        _files.Where(f => f.Kind == kind && !f.IsDeleted).OrderByDescending(f => f.Version).FirstOrDefault();

    /// <summary>The newest version of an AI output for a stage, whatever its status.</summary>
    public ArtifactVersion? LatestArtifact(ArtifactStage stage) =>
        _artifacts.Where(a => a.Stage == stage).OrderByDescending(a => a.Version).FirstOrDefault();

    /// <summary>A stage can be drafted (or redrafted) only while the project is at the stage just before its gate.</summary>
    public bool CanDraft(ArtifactStage stage) => Stage == StageRules.RequiredProjectStage(stage);

    /// <summary>Analysis can be drafted (or redrafted) once both inputs are in and until gate 1 is passed.</summary>
    public bool CanDraftAnalysis => CanDraft(ArtifactStage.Analysis);

    /// <summary>Adds a new version of a stage's output; any draft still waiting for review is superseded.</summary>
    public ArtifactVersion AddDraft(
        ArtifactStage stage,
        string contentJson,
        string? notesJson,
        ArtifactSource source,
        string model,
        string promptVersion,
        string? feedback,
        Guid createdBy,
        DateTimeOffset now)
    {
        if (!CanDraft(stage))
        {
            throw new InvalidOperationException($"The {stage} stage cannot be drafted at this point in the project.");
        }

        var previous = LatestArtifact(stage);
        foreach (var pending in _artifacts.Where(a => a.Stage == stage))
        {
            pending.Supersede();
        }

        var draft = new ArtifactVersion
        {
            Id = Guid.NewGuid(),
            ProjectId = Id,
            Stage = stage,
            Version = (previous?.Version ?? 0) + 1,
            Source = source,
            ContentJson = contentJson,
            NotesJson = notesJson,
            Model = model,
            PromptVersion = promptVersion,
            Feedback = feedback,
            BasedOnVersion = previous?.Version,
            CreatedById = createdBy,
            CreatedAtUtc = now,
        };
        _artifacts.Add(draft);
        return draft;
    }

    /// <summary>Adds a new draft of the analysis.</summary>
    public ArtifactVersion AddAnalysisDraft(
        string contentJson,
        string? notesJson,
        ArtifactSource source,
        string model,
        string promptVersion,
        string? feedback,
        Guid createdBy,
        DateTimeOffset now) =>
        AddDraft(ArtifactStage.Analysis, contentJson, notesJson, source, model, promptVersion, feedback, createdBy, now);

    /// <summary>Approving the newest pending version moves the project past that stage's gate.</summary>
    public GateOutcome Approve(ArtifactStage stage, int version, Guid reviewerId, string reviewerRole, string? comment, DateTimeOffset now)
    {
        var outcome = CheckDecision(stage, version, out var draft);
        if (outcome != GateOutcome.Done)
        {
            return outcome;
        }

        draft!.Decide(ReviewDecisionKind.Approved, reviewerId, reviewerRole, comment, now);
        Stage = StageRules.ApprovedProjectStage(stage);
        return GateOutcome.Done;
    }

    /// <summary>Rejecting keeps the project where it is; the reviewer can ask for a redraft.</summary>
    public GateOutcome Reject(ArtifactStage stage, int version, Guid reviewerId, string reviewerRole, string comment, DateTimeOffset now)
    {
        var outcome = CheckDecision(stage, version, out var draft);
        if (outcome != GateOutcome.Done)
        {
            return outcome;
        }

        draft!.Decide(ReviewDecisionKind.Rejected, reviewerId, reviewerRole, comment, now);
        return GateOutcome.Done;
    }

    /// <summary>Gate 1: approving the newest pending analysis moves the project on to architecture.</summary>
    public GateOutcome ApproveAnalysis(int version, Guid reviewerId, string reviewerRole, string? comment, DateTimeOffset now) =>
        Approve(ArtifactStage.Analysis, version, reviewerId, reviewerRole, comment, now);

    /// <summary>Gate 1: rejecting keeps the project where it is; the reviewer can ask for a redraft.</summary>
    public GateOutcome RejectAnalysis(int version, Guid reviewerId, string reviewerRole, string comment, DateTimeOffset now) =>
        Reject(ArtifactStage.Analysis, version, reviewerId, reviewerRole, comment, now);

    private GateOutcome CheckDecision(ArtifactStage stage, int version, out ArtifactVersion? draft)
    {
        draft = _artifacts.Find(a => a.Stage == stage && a.Version == version);
        if (draft is null)
        {
            return GateOutcome.NotFound;
        }

        if (!CanDraft(stage))
        {
            return GateOutcome.WrongStage;
        }

        if (LatestArtifact(stage)!.Version != version)
        {
            return GateOutcome.NotLatest;
        }

        return draft.Status == ArtifactStatus.PendingReview ? GateOutcome.Done : GateOutcome.NotPending;
    }

    private void RefreshStage()
    {
        // Past gate 1 the stage is decided by approvals, not by which files are present.
        if (Stage >= ProjectStage.AnalysisApproved)
        {
            return;
        }

        var hasBoth = LatestFile(FileKind.Requirements) is not null && LatestFile(FileKind.TeamTemplate) is not null;
        Stage = hasBoth ? ProjectStage.FilesUploaded : ProjectStage.Draft;
    }
}
