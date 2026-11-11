namespace AiSdlc.Domain.Auditing;

/// <summary>One append-only row of the audit trail. Never holds secrets or passwords.</summary>
public sealed class AuditEntry
{
    public Guid Id { get; init; }

    public DateTimeOffset OccurredAtUtc { get; init; }

    public Guid? ActorId { get; init; }

    public string? ActorEmail { get; init; }

    public required string Action { get; init; }

    public required AuditOutcome Outcome { get; init; }

    public string? Details { get; init; }
}
