using AiSdlc.Application.Analysis.Agents;
using AiSdlc.Application.Analysis.Content;
using AiSdlc.Domain.Artifacts;

namespace AiSdlc.Application.Analysis;

internal static class AnalysisMapper
{
    private static readonly AnalysisNotes NoNotes = new([], []);

    public static AnalysisVersionDto ToDto(ArtifactVersion version) =>
        new(
            version.Version,
            version.Status,
            version.Source,
            version.Model,
            version.PromptVersion,
            version.Feedback,
            version.BasedOnVersion,
            version.CreatedAtUtc,
            AnalysisJson.Deserialize<AnalysisContent>(version.ContentJson)
                ?? throw new InvalidOperationException($"Stored analysis v{version.Version} has no readable content."),
            version.NotesJson is null ? NoNotes : AnalysisJson.Deserialize<AnalysisNotes>(version.NotesJson) ?? NoNotes,
            version.Decisions
                .OrderBy(d => d.DecidedAtUtc)
                .Select(d => new ReviewDecisionDto(d.Kind, d.ReviewerId, d.ReviewerRole, d.Comment, d.DecidedAtUtc))
                .ToList());
}
