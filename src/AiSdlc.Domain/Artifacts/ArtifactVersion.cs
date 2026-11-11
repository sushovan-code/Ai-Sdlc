namespace AiSdlc.Domain.Artifacts;

/// <summary>
/// One draft of an AI output for a project stage. Drafts are never overwritten: a regeneration is a new version, and
/// the feedback that asked for it is kept with it. Decisions are only made through the <c>Project</c> aggregate.
/// </summary>
public sealed class ArtifactVersion
{
    private readonly List<ReviewDecision> _decisions = [];

    public Guid Id { get; init; }

    public Guid ProjectId { get; init; }

    public ArtifactStage Stage { get; init; }

    public int Version { get; init; }

    public ArtifactStatus Status { get; private set; } = ArtifactStatus.PendingReview;

    public ArtifactSource Source { get; init; }

    /// <summary>The structured content as JSON, including the evidence behind each item.</summary>
    public required string ContentJson { get; init; }

    /// <summary>Critic notes and guardrail flags as JSON.</summary>
    public string? NotesJson { get; init; }

    /// <summary>The model that wrote it (for AI versions).</summary>
    public required string Model { get; init; }

    /// <summary>The prompt revision used, so a result can be traced to the instructions that produced it.</summary>
    public required string PromptVersion { get; init; }

    /// <summary>The reviewer comment that asked for this version, when it is a regeneration.</summary>
    public string? Feedback { get; init; }

    public int? BasedOnVersion { get; init; }

    public Guid CreatedById { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; }

    public IReadOnlyCollection<ReviewDecision> Decisions => _decisions;

    internal void Supersede()
    {
        if (Status == ArtifactStatus.PendingReview)
        {
            Status = ArtifactStatus.Superseded;
        }
    }

    internal void Decide(ReviewDecisionKind kind, Guid reviewerId, string reviewerRole, string? comment, DateTimeOffset now)
    {
        Status = kind == ReviewDecisionKind.Approved ? ArtifactStatus.Approved : ArtifactStatus.Rejected;
        _decisions.Add(new ReviewDecision
        {
            Id = Guid.NewGuid(),
            ArtifactVersionId = Id,
            Kind = kind,
            ReviewerId = reviewerId,
            ReviewerRole = reviewerRole,
            Comment = comment,
            DecidedAtUtc = now,
        });
    }
}
