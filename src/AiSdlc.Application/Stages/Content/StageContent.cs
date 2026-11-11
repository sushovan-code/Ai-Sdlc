namespace AiSdlc.Application.Stages.Content;

/// <summary>
/// The shared shape of the architecture, task and test-case outputs: a summary and titled sections of items. One shape
/// keeps review, editing, traceability and the report the same for every stage.
/// </summary>
public sealed record StageContent(string Summary, IReadOnlyList<StageSection> Sections);
