using AiSdlc.Domain.Artifacts;

namespace AiSdlc.Application.Analysis;

/// <summary>Who decided a version, in which role, when and why.</summary>
public sealed record ReviewDecisionDto(
    ReviewDecisionKind Kind,
    Guid ReviewerId,
    string ReviewerRole,
    string? Comment,
    DateTimeOffset DecidedAtUtc);
