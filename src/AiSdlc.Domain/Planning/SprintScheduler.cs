namespace AiSdlc.Domain.Planning;

/// <summary>
/// Places tasks into sprints, deterministically. Tasks are ordered by dependency, then priority, then their original
/// order. Each task goes to the best skill match who still has room in the earliest possible sprint; when a sprint is
/// full the work spills over into the next, until every placeable task has an owner. A task is never split: one task,
/// one person, one sprint. Tasks that cannot be placed are reported with the reason.
/// </summary>
public static class SprintScheduler
{
    /// <summary>A safety bound so a logic error can never loop forever.</summary>
    private const int MaxSprints = 520;

    public static SchedulePlan Schedule(IReadOnlyList<SchedulableTask> tasks, IReadOnlyList<SprintMember> members)
    {
        var byId = BuildIndex(tasks);
        var workers = members.Where(m => m.SprintCapacityHours > 0).ToList();
        var state = new PlacementState();

        foreach (var task in InPlacementOrder(tasks, byId, state))
        {
            var reason = TryPlace(task, byId, workers, state);
            if (reason is not null)
            {
                state.Unassigned[task.Id] = reason.Value;
            }
        }

        var unassigned = tasks
            .Where(t => state.Unassigned.ContainsKey(t.Id))
            .Select(t => new UnassignedTask(t.Id, state.Unassigned[t.Id]))
            .ToList();
        return new SchedulePlan(state.Assignments, unassigned);
    }

    private static Dictionary<Guid, SchedulableTask> BuildIndex(IReadOnlyList<SchedulableTask> tasks)
    {
        var byId = new Dictionary<Guid, SchedulableTask>(tasks.Count);
        foreach (var task in tasks)
        {
            if (!byId.TryAdd(task.Id, task))
            {
                throw new ArgumentException($"Task id {task.Id} appears more than once.", nameof(tasks));
            }
        }

        return byId;
    }

    /// <summary>
    /// Dependencies first; among tasks that are ready, higher priority first, then original order.
    /// Tasks stuck in a dependency loop are marked and left out.
    /// </summary>
    private static List<SchedulableTask> InPlacementOrder(
        IReadOnlyList<SchedulableTask> tasks, Dictionary<Guid, SchedulableTask> byId, PlacementState state)
    {
        var index = tasks.Select((task, position) => (task, position)).ToDictionary(x => x.task.Id, x => x.position);
        var waitingOn = tasks.ToDictionary(
            t => t.Id, t => t.DependsOn.Where(byId.ContainsKey).Distinct().Count());
        var dependents = new Dictionary<Guid, List<Guid>>();
        foreach (var task in tasks)
        {
            foreach (var dependency in task.DependsOn.Where(byId.ContainsKey).Distinct())
            {
                dependents.TryAdd(dependency, []);
                dependents[dependency].Add(task.Id);
            }
        }

        var ready = tasks.Where(t => waitingOn[t.Id] == 0).ToList();
        var ordered = new List<SchedulableTask>(tasks.Count);
        while (ready.Count > 0)
        {
            var next = ready.OrderBy(t => t.Priority).ThenBy(t => index[t.Id]).First();
            ready.Remove(next);
            ordered.Add(next);

            foreach (var dependentId in dependents.GetValueOrDefault(next.Id) ?? [])
            {
                if (--waitingOn[dependentId] == 0)
                {
                    ready.Add(byId[dependentId]);
                }
            }
        }

        foreach (var stuck in tasks.Where(t => waitingOn[t.Id] > 0))
        {
            state.Unassigned[stuck.Id] = UnassignedReason.CircularDependency;
        }

        return ordered;
    }

    private static UnassignedReason? TryPlace(
        SchedulableTask task,
        Dictionary<Guid, SchedulableTask> byId,
        List<SprintMember> workers,
        PlacementState state)
    {
        var blocked = CheckBlockers(task, byId, workers, state);
        if (blocked is not null)
        {
            return blocked;
        }

        var candidates = workers
            .Select(w => (Member: w, Score: SkillScore(w, task)))
            .Where(c => task.RequiredSkills.Count == 0 || c.Score > 0)
            .ToList();
        if (candidates.Count == 0)
        {
            return UnassignedReason.NoMatchingSkill;
        }

        if (candidates.TrueForAll(c => c.Member.SprintCapacityHours < task.Hours))
        {
            return UnassignedReason.ExceedsSprintCapacity;
        }

        var earliest = task.DependsOn.Select(d => state.SprintOf[d]).DefaultIfEmpty(1).Max();
        for (var sprint = earliest; sprint <= MaxSprints; sprint++)
        {
            var owner = candidates
                .Where(c => state.Remaining(c.Member, sprint) >= task.Hours)
                .OrderByDescending(c => c.Score)
                .ThenByDescending(c => state.Remaining(c.Member, sprint))
                .ThenBy(c => c.Member.Name, StringComparer.Ordinal)
                .ThenBy(c => c.Member.Id)
                .Select(c => c.Member)
                .FirstOrDefault();

            if (owner is not null)
            {
                state.Assign(task, owner, sprint);
                return null;
            }
        }

        return UnassignedReason.ExceedsSprintCapacity;
    }

    private static UnassignedReason? CheckBlockers(
        SchedulableTask task, Dictionary<Guid, SchedulableTask> byId, List<SprintMember> workers, PlacementState state)
    {
        if (task.Hours <= 0)
        {
            return UnassignedReason.InvalidEstimate;
        }

        if (task.DependsOn.Any(d => !byId.ContainsKey(d)))
        {
            return UnassignedReason.UnknownDependency;
        }

        if (task.DependsOn.Any(state.Unassigned.ContainsKey))
        {
            return UnassignedReason.DependencyUnassigned;
        }

        return workers.Count == 0 ? UnassignedReason.NoCapacity : null;
    }

    private static int SkillScore(SprintMember member, SchedulableTask task) =>
        task.RequiredSkills
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count(skill => member.Skills.Contains(skill, StringComparer.OrdinalIgnoreCase));

    private sealed class PlacementState
    {
        private readonly Dictionary<(Guid Member, int Sprint), decimal> _used = [];

        public List<Assignment> Assignments { get; } = [];

        public Dictionary<Guid, int> SprintOf { get; } = [];

        public Dictionary<Guid, UnassignedReason> Unassigned { get; } = [];

        public decimal Remaining(SprintMember member, int sprint) =>
            member.SprintCapacityHours - _used.GetValueOrDefault((member.Id, sprint));

        public void Assign(SchedulableTask task, SprintMember member, int sprint)
        {
            _used[(member.Id, sprint)] = _used.GetValueOrDefault((member.Id, sprint)) + task.Hours;
            SprintOf[task.Id] = sprint;
            Assignments.Add(new Assignment(task.Id, member.Id, sprint, task.Hours));
        }
    }
}
