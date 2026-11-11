using AiSdlc.Application.Stages.Content;
using AiSdlc.Domain.Planning;
using AiSdlc.Domain.Projects;

namespace AiSdlc.Application.Stages.Agents;

/// <summary>
/// The model proposes tasks; this places them. It hands them to the deterministic <see cref="SprintScheduler"/> with the
/// team's sprint capacity and turns the plan into reviewable items plus the assignments the team page needs.
/// </summary>
public static class TaskPlanner
{
    public static (IReadOnlyList<StageItem> Items, IReadOnlyList<PlannedAssignment> Assignments) Plan(
        IReadOnlyList<TaskDraft> drafts, IReadOnlyList<TeamMember> team, int sprintWeeks)
    {
        var keys = drafts.Select((d, index) => (d.Id, Key: KeyFor(index))).ToDictionary(x => x.Id, x => x.Key, StringComparer.OrdinalIgnoreCase);
        var tasks = drafts.Select(d => new SchedulableTask(
            keys[d.Id],
            d.Title,
            d.Hours,
            Enum.Parse<TaskPriority>(d.Priority),
            d.Skills,
            d.DependsOn.Select(dep => keys.TryGetValue(dep, out var key) ? key : Guid.NewGuid()).ToList())).ToList();
        var members = team
            .Select(m => new SprintMember(m.Id, m.Name, m.Skills, SprintRules.SprintHours(m.AvailabilityHours, sprintWeeks)))
            .ToList();

        var plan = SprintScheduler.Schedule(tasks, members);
        var names = team.ToDictionary(m => m.Id, m => m.Name);
        var byKey = drafts.ToDictionary(d => keys[d.Id]);

        var assignments = plan.Assignments
            .OrderBy(a => a.Sprint).ThenBy(a => byKey[a.TaskId].Id, StringComparer.OrdinalIgnoreCase)
            .Select(a => new PlannedAssignment(byKey[a.TaskId].Id, a.MemberId, names[a.MemberId], a.Sprint, a.Hours))
            .ToList();
        var unassigned = plan.Unassigned.ToDictionary(u => u.TaskId, u => u.Reason);

        var items = drafts
            .Select(d => (Draft: d, Assignment: assignments.FirstOrDefault(a => a.TaskId == d.Id)))
            .OrderBy(x => x.Assignment?.Sprint ?? int.MaxValue).ThenBy(x => x.Draft.Id, StringComparer.OrdinalIgnoreCase)
            .Select(x => new StageItem(
                x.Draft.Id,
                x.Draft.Title,
                x.Draft.Description,
                Tags(x.Draft, x.Assignment, unassigned.GetValueOrDefault(keys[x.Draft.Id])),
                x.Draft.Refs))
            .ToList();

        return (items, assignments);
    }

    /// <summary>A short, readable explanation of why a task could not be placed.</summary>
    public static string Explain(UnassignedReason reason) => reason switch
    {
        UnassignedReason.InvalidEstimate => "needs a valid estimate",
        UnassignedReason.NoMatchingSkill => "nobody has the skills",
        UnassignedReason.NoCapacity => "nobody has capacity",
        UnassignedReason.ExceedsSprintCapacity => "too big for one sprint, split it",
        UnassignedReason.UnknownDependency => "waits for an unknown task",
        UnassignedReason.CircularDependency => "part of a dependency loop",
        UnassignedReason.DependencyUnassigned => "waits for a task that could not be placed",
        _ => "could not be placed",
    };

    private static List<string> Tags(TaskDraft draft, PlannedAssignment? assignment, UnassignedReason reason)
    {
        var tags = new List<string> { $"{draft.Hours:0.##}h", draft.Priority };
        tags.Add(assignment is null
            ? $"Unassigned: {Explain(reason)}"
            : $"Sprint {assignment.Sprint}: {assignment.MemberName}");
        return tags;
    }

    private static Guid KeyFor(int index) => new(index + 1, 0, 0, [0, 0, 0, 0, 0, 0, 0, 1]);
}
