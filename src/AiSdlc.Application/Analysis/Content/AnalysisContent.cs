namespace AiSdlc.Application.Analysis.Content;

/// <summary>The structured requirements analysis: what the system must do, how well, and what is unclear or risky.</summary>
public sealed record AnalysisContent(
    string Summary,
    IReadOnlyList<RequirementItem> FunctionalRequirements,
    IReadOnlyList<RequirementItem> NonFunctionalRequirements,
    IReadOnlyList<QuestionItem> Ambiguities,
    IReadOnlyList<QuestionItem> Gaps,
    IReadOnlyList<string> Assumptions,
    IReadOnlyList<RiskItem> Risks,
    IReadOnlyList<Evidence> Evidence);
