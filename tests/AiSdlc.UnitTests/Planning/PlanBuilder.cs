using AiSdlc.Domain.Planning;

namespace AiSdlc.UnitTests.Planning;

/// <summary>Builds scheduler input by name, so tests read like the plan they describe.</summary>
public sealed class PlanBuilder
{
    private readonly Dictionary<string, Guid> _ids = [];
    private readonly List<SchedulableTask> _tasks = [];
    private readonly List<SprintMember> _members = [];

    public IReadOnlyList<SchedulableTask> Tasks => _tasks;

    public IReadOnlyList<SprintMember> Members => _members;

    public Guid Id(string name)
    {
        if (!_ids.TryGetValue(name, out var id))
        {
            id = Guid.NewGuid();
            _ids[name] = id;
        }

        return id;
    }

    public PlanBuilder Member(string name, decimal sprintHours, params string[] skills)
    {
        _members.Add(new SprintMember(Id(name), name, skills, sprintHours));
        return this;
    }

    public PlanBuilder Task(
        string name,
        decimal hours,
        TaskPriority priority = TaskPriority.Medium,
        string[]? skills = null,
        string[]? dependsOn = null)
    {
        _tasks.Add(new SchedulableTask(
            Id(name),
            name,
            hours,
            priority,
            skills ?? [],
            (dependsOn ?? []).Select(Id).ToList()));
        return this;
    }

    public SchedulePlan Schedule() => SprintScheduler.Schedule(_tasks, _members);

    /// <summary>Which person and sprint a task landed in.</summary>
    public (string Member, int Sprint) Placement(SchedulePlan plan, string task)
    {
        var assignment = plan.Assignments.Single(a => a.TaskId == Id(task));
        return (_members.Single(m => m.Id == assignment.MemberId).Name, assignment.Sprint);
    }

    public UnassignedReason? UnassignedReasonOf(SchedulePlan plan, string task) =>
        plan.Unassigned.SingleOrDefault(u => u.TaskId == Id(task))?.Reason;
}
