using AiSdlc.Application.Analysis.Content;

namespace AiSdlc.Application.Analysis.Agents;

public sealed record SpecialistOutput(AnalysisContent Content, string Model, long? TotalTokens);
