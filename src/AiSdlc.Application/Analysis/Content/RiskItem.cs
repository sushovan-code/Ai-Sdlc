namespace AiSdlc.Application.Analysis.Content;

public sealed record RiskItem(string Id, string Description, string Severity, IReadOnlyList<string> SourceRefs);
