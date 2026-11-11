namespace AiSdlc.Domain.Planning;

/// <summary>Why a task could not be placed. The Project Manager resolves these, for example by splitting the task.</summary>
public enum UnassignedReason
{
    /// <summary>The estimate is zero or negative.</summary>
    InvalidEstimate,

    /// <summary>Nobody on the team has any of the skills the task needs.</summary>
    NoMatchingSkill,

    /// <summary>Nobody has capacity at all (everyone is at zero available hours).</summary>
    NoCapacity,

    /// <summary>The task is bigger than anyone's sprint, so it can never fit and should be split.</summary>
    ExceedsSprintCapacity,

    /// <summary>The task waits for something that does not exist.</summary>
    UnknownDependency,

    /// <summary>The task is part of a dependency loop.</summary>
    CircularDependency,

    /// <summary>A task it waits for could not be placed.</summary>
    DependencyUnassigned,
}
