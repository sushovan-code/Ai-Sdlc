namespace AiSdlc.Domain.Artifacts;

/// <summary>A reviewer's decision on one version: who, in which role, when, and why. Part of the audit trail.</summary>
public sealed class ReviewDecision
{
    public Guid Id { get; init; }

    public Guid ArtifactVersionId { get; init; }

    public ReviewDecisionKind Kind { get; init; }

    public Guid ReviewerId { get; init; }

    public required string ReviewerRole { get; init; }

    public string? Comment { get; init; }

    public DateTimeOffset DecidedAtUtc { get; init; }
}
