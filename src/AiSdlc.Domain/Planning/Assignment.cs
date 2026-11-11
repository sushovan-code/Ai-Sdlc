namespace AiSdlc.Domain.Planning;

/// <summary>One task given to one person in one sprint (sprints are numbered from 1).</summary>
public sealed record Assignment(Guid TaskId, Guid MemberId, int Sprint, decimal Hours);
