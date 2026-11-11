using AiSdlc.Application.Analysis.Content;

namespace AiSdlc.Application.Analysis.Agents;

/// <summary>A finished draft: the analysis with evidence, the critic and governance notes, and the model that wrote it.</summary>
public sealed record AnalysisResult(AnalysisContent Content, AnalysisNotes Notes, string Model);
