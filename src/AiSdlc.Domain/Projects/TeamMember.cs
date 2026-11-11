namespace AiSdlc.Domain.Projects;

/// <summary>A person on the uploaded team sheet. They are capacity to plan with, not a login account.</summary>
public sealed class TeamMember
{
    public Guid Id { get; init; }

    public Guid ProjectId { get; init; }

    /// <summary>The team sheet version this row came from.</summary>
    public Guid ProjectFileId { get; init; }

    public required string Name { get; init; }

    public required string Role { get; init; }

    public required IReadOnlyList<string> Skills { get; init; }

    /// <summary>Hours the person can work in a 2-week window.</summary>
    public decimal CapacityHours { get; init; }

    /// <summary>Hours actually available in a 2-week window (never more than the capacity).</summary>
    public decimal AvailabilityHours { get; init; }
}
