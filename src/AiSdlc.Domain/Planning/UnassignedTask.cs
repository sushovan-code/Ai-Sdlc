namespace AiSdlc.Domain.Planning;

public sealed record UnassignedTask(Guid TaskId, UnassignedReason Reason);
