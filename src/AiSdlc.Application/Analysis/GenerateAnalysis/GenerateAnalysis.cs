namespace AiSdlc.Application.Analysis.GenerateAnalysis;

/// <summary>Drafts the analysis. <paramref name="Feedback"/> is the reviewer's comment when regenerating.</summary>
public sealed record GenerateAnalysis(Guid ProjectId, string? Feedback);
