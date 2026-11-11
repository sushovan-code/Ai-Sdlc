namespace AiSdlc.Domain.Planning;

/// <summary>How much of a person's time is booked: total hours given to them against their capacity over the plan.</summary>
public sealed record MemberUtilization(
    Guid MemberId,
    decimal AllocatedHours,
    decimal TotalCapacityHours,
    IReadOnlyList<MemberSprintLoad> Sprints)
{
    /// <summary>Booked hours as a percentage of capacity. It can exceed 100 only if work was forced beyond capacity.</summary>
    public decimal Percent => TotalCapacityHours == 0 ? 0 : AllocatedHours / TotalCapacityHours * 100;
}
