namespace AiSdlc.Application.Abstractions;

/// <summary>One data row of the team sheet exactly as read from the file, before any checking.</summary>
public sealed record RawTeamRow(
    int RowNumber,
    string? Name,
    string? Role,
    string? Skills,
    string? Capacity,
    string? Availability);
