namespace AiSdlc.Application.Stages.Content;

/// <summary>One thing in a stage output: a component, a task, a test case. Refs point at items it traces back to.</summary>
public sealed record StageItem(
    string Id,
    string Title,
    string Text,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> Refs);
