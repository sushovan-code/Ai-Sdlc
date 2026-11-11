using AiSdlc.Application.Abstractions;
using AiSdlc.Application.Common;
using AiSdlc.Application.Projects;
using AiSdlc.Domain.Planning;

namespace AiSdlc.Application.Team.GetTeam;

/// <summary>The people on the project's current team sheet with their capacity and how much of it is booked.</summary>
public sealed class GetTeamHandler(ICurrentUser currentUser, IProjectRepository projects)
{
    public async Task<Result<TeamOverviewDto>> HandleAsync(GetTeam query, CancellationToken cancellationToken)
    {
        var opened = await ProjectAccess.OpenAsync(projects, currentUser, query.ProjectId, false, cancellationToken);
        if (opened.IsFailure)
        {
            return Result<TeamOverviewDto>.Failure(opened.Error);
        }

        var project = opened.Value;
        var team = project.CurrentTeam;
        var members = team
            .Select(m => new SprintMember(m.Id, m.Name, m.Skills, SprintRules.SprintHours(m.AvailabilityHours, project.SprintWeeks)))
            .ToList();

        // Work is given to people by the task slice; until tasks are scheduled nothing is booked yet.
        var utilization = UtilizationCalculator.Calculate(members, []).ToDictionary(u => u.MemberId);

        var rows = team.Select(m =>
        {
            var load = utilization[m.Id];
            return new TeamMemberDto(
                m.Id,
                m.Name,
                m.Role,
                m.Skills,
                m.CapacityHours,
                m.AvailabilityHours,
                SprintRules.SprintHours(m.AvailabilityHours, project.SprintWeeks),
                load.AllocatedHours,
                load.TotalCapacityHours,
                load.Percent,
                load.Sprints.Select(s => new SprintLoadDto(s.Sprint, s.AllocatedHours, s.CapacityHours)).ToList());
        }).OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase).ToList();

        return Result<TeamOverviewDto>.Success(new TeamOverviewDto(
            project.SprintWeeks,
            SprintRules.WorkingHoursPerDay,
            rows.Select(r => r.Sprints.Count).DefaultIfEmpty(1).Max(),
            rows.Sum(r => r.AllocatedHours),
            rows.Sum(r => r.TotalCapacityHours),
            rows));
    }
}
