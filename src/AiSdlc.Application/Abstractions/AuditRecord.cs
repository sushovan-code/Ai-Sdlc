using AiSdlc.Domain.Auditing;

namespace AiSdlc.Application.Abstractions;

/// <summary>What a use case reports to the audit trail; the adapter adds the id and timestamp.</summary>
public sealed record AuditRecord(
    string Action,
    AuditOutcome Outcome,
    Guid? ActorId = null,
    string? ActorEmail = null,
    string? Details = null);
