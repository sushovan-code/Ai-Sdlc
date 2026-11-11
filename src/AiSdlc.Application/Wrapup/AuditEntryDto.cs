using AiSdlc.Domain.Auditing;

namespace AiSdlc.Application.Wrapup;

/// <summary>One line of a project audit trail: who did what, when, and how it went.</summary>
public sealed record AuditEntryDto(
    DateTimeOffset OccurredAtUtc,
    string? ActorEmail,
    string Action,
    AuditOutcome Outcome,
    string? Details);
