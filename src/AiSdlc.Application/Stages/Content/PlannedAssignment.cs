namespace AiSdlc.Application.Stages.Content;

/// <summary>A task given to a person in a sprint by the scheduler. Kept with the task draft so the team page can show load.</summary>
public sealed record PlannedAssignment(string TaskId, Guid MemberId, string MemberName, int Sprint, decimal Hours);
