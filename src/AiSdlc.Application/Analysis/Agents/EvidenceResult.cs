using AiSdlc.Application.Analysis.Content;

namespace AiSdlc.Application.Analysis.Agents;

/// <summary>The analysis with its evidence attached, and the items that have none.</summary>
public sealed record EvidenceResult(AnalysisContent Content, IReadOnlyList<string> UnsupportedItemIds);
