namespace AiSdlc.Application.Stages.Agents;

/// <summary>A task as the model proposed it, before the scheduler places it.</summary>
public sealed record TaskDraft(
    string Id,
    string Title,
    string Description,
    decimal Hours,
    string Priority,
    IReadOnlyList<string> Skills,
    IReadOnlyList<string> DependsOn,
    IReadOnlyList<string> Refs);
