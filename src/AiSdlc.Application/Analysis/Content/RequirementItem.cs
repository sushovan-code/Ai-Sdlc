namespace AiSdlc.Application.Analysis.Content;

/// <summary>A functional or non-functional requirement found in the source, with the chunks it was drawn from.</summary>
public sealed record RequirementItem(
    string Id,
    string Title,
    string Description,
    string Priority,
    IReadOnlyList<string> SourceRefs);
