namespace AiSdlc.Application.Team.GetTeam;

/// <summary>Hours booked and available for one person in one sprint.</summary>
public sealed record SprintLoadDto(int Sprint, decimal AllocatedHours, decimal CapacityHours);
