namespace AiSdlc.Application.Analysis.Content;

/// <summary>What the critic and the governance agent said about a draft; shown next to it for the reviewer.</summary>
public sealed record AnalysisNotes(IReadOnlyList<CriticNote> Critic, IReadOnlyList<GuardrailFlag> Flags);
