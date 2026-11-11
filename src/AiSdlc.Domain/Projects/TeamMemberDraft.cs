namespace AiSdlc.Domain.Projects;

/// <summary>A validated row of the team sheet, before it is attached to a stored file version.</summary>
public sealed record TeamMemberDraft(
    string Name,
    string Role,
    IReadOnlyList<string> Skills,
    decimal CapacityHours,
    decimal AvailabilityHours);
