namespace AiSdlc.Domain.Planning;

/// <summary>A person the scheduler can give work to, with the hours they have in one sprint.</summary>
public sealed record SprintMember(Guid Id, string Name, IReadOnlyCollection<string> Skills, decimal SprintCapacityHours);
