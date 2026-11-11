namespace AiSdlc.Application.Analysis.Content;

/// <summary>An ambiguity or gap: something the source leaves unclear or missing.</summary>
public sealed record QuestionItem(string Id, string Text, IReadOnlyList<string> SourceRefs);
