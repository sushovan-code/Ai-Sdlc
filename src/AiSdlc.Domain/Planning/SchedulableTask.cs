namespace AiSdlc.Domain.Planning;

/// <summary>A piece of work to place: how long it takes, what skills it needs and what it waits for.</summary>
public sealed record SchedulableTask(
    Guid Id,
    string Title,
    decimal Hours,
    TaskPriority Priority,
    IReadOnlyCollection<string> RequiredSkills,
    IReadOnlyCollection<Guid> DependsOn);
