namespace AiSdlc.Application.Team.GetTeam;

public sealed record TeamOverviewDto(
    int SprintWeeks,
    int HoursPerDay,
    int SprintCount,
    decimal TotalAllocatedHours,
    decimal TotalCapacityHours,
    IReadOnlyList<TeamMemberDto> Members);
