using AiSdlc.Application.Analysis;
using AiSdlc.Application.Analysis.Agents;
using AiSdlc.Application.Stages.Content;
using AiSdlc.Domain.Artifacts;

namespace AiSdlc.Application.Stages;

internal static class StageMapper
{
    private static readonly StageNotes NoNotes = new([], [], []);

    public static StageContent ReadContent(ArtifactVersion version) =>
        AnalysisJson.Deserialize<StageContent>(version.ContentJson)
            ?? throw new InvalidOperationException($"Stored {version.Stage} v{version.Version} has no readable content.");

    public static StageNotes ReadNotes(ArtifactVersion version) =>
        version.NotesJson is null ? NoNotes : AnalysisJson.Deserialize<StageNotes>(version.NotesJson) ?? NoNotes;

    public static StageVersionDto ToDto(ArtifactVersion version) =>
        new(
            version.Version,
            version.Status,
            version.Source,
            version.Model,
            version.PromptVersion,
            version.Feedback,
            version.BasedOnVersion,
            version.CreatedAtUtc,
            ReadContent(version),
            ReadNotes(version),
            version.Decisions
                .OrderBy(d => d.DecidedAtUtc)
                .Select(d => new ReviewDecisionDto(d.Kind, d.ReviewerId, d.ReviewerRole, d.Comment, d.DecidedAtUtc))
                .ToList());
}
