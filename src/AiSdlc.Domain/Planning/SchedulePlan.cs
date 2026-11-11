namespace AiSdlc.Domain.Planning;

/// <summary>The result of scheduling: who does what in which sprint, and what could not be placed.</summary>
public sealed record SchedulePlan(IReadOnlyList<Assignment> Assignments, IReadOnlyList<UnassignedTask> Unassigned)
{
    /// <summary>How many sprints the plan spans (0 when nothing was placed).</summary>
    public int SprintCount => Assignments.Count == 0 ? 0 : Assignments.Max(a => a.Sprint);
}
