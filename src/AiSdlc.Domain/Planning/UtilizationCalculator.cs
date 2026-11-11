namespace AiSdlc.Domain.Planning;

public static class UtilizationCalculator
{
    /// <summary>
    /// Booked hours against capacity for each member. Capacity counts every sprint the plan spans (at least one),
    /// so a person with room left in the last sprint shows as under-used rather than full.
    /// </summary>
    public static IReadOnlyList<MemberUtilization> Calculate(
        IReadOnlyList<SprintMember> members, IReadOnlyList<Assignment> assignments)
    {
        var sprintCount = Math.Max(1, assignments.Select(a => a.Sprint).DefaultIfEmpty(1).Max());

        return members.Select(member =>
        {
            var mine = assignments.Where(a => a.MemberId == member.Id).ToList();
            var sprints = Enumerable.Range(1, sprintCount)
                .Select(sprint => new MemberSprintLoad(
                    sprint,
                    mine.Where(a => a.Sprint == sprint).Sum(a => a.Hours),
                    member.SprintCapacityHours))
                .ToList();

            return new MemberUtilization(
                member.Id,
                mine.Sum(a => a.Hours),
                member.SprintCapacityHours * sprintCount,
                sprints);
        }).ToList();
    }
}
