namespace AiSdlc.Application.Team.GetTeam;

/// <summary>
/// One person from the team sheet. <c>CapacityHours</c> and <c>AvailabilityHours</c> are the sheet's figures for a 2-week
/// window; <c>SprintCapacityHours</c> scales availability to the project's sprint length; <c>AllocatedHours</c> is the work
/// given to them across the plan and <c>TotalCapacityHours</c> what they have across every sprint the plan spans.
/// </summary>
public sealed record TeamMemberDto(
    Guid Id,
    string Name,
    string Role,
    IReadOnlyList<string> Skills,
    decimal CapacityHours,
    decimal AvailabilityHours,
    decimal SprintCapacityHours,
    decimal AllocatedHours,
    decimal TotalCapacityHours,
    decimal UtilizationPercent,
    IReadOnlyList<SprintLoadDto> Sprints);
