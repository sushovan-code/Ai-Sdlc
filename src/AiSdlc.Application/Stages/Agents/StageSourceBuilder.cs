using System.Text;
using AiSdlc.Application.Analysis;
using AiSdlc.Application.Analysis.Agents;
using AiSdlc.Application.Analysis.Content;
using AiSdlc.Application.Common;
using AiSdlc.Application.Stages.Content;
using AiSdlc.Domain.Artifacts;
using AiSdlc.Domain.Projects;

namespace AiSdlc.Application.Stages.Agents;

/// <summary>Collects the approved earlier output a stage is drafted from, as prompt text plus the ids it may cite.</summary>
public static class StageSourceBuilder
{
    public static Result<StageSource> Build(Project project, ArtifactStage stage)
    {
        var text = new StringBuilder();
        var known = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var analysis = Approved(project, ArtifactStage.Analysis);
        var content = analysis is null ? null : AnalysisJson.Deserialize<AnalysisContent>(analysis.ContentJson);
        if (content is null)
        {
            return Result<StageSource>.Failure(AnalysisErrors.NotAvailable("The approved requirements analysis is needed first."));
        }

        text.AppendLine("Requirements:");
        foreach (var requirement in content.FunctionalRequirements.Concat(content.NonFunctionalRequirements))
        {
            Line(text, known, requirement.Id, requirement.Title, requirement.Description, []);
        }

        if (stage is ArtifactStage.Tasks or ArtifactStage.TestCases)
        {
            AppendApproved(project, ArtifactStage.Architecture, "Architecture:", text, known);
        }

        if (stage == ArtifactStage.Tasks)
        {
            var skills = project.CurrentTeam.SelectMany(m => m.Skills).Distinct(StringComparer.OrdinalIgnoreCase).Order().ToList();
            text.AppendLine().Append("Team skills: ").AppendLine(string.Join(", ", skills));
        }

        if (stage == ArtifactStage.TestCases)
        {
            AppendApproved(project, ArtifactStage.Tasks, "Tasks:", text, known);
        }

        return Result<StageSource>.Success(new StageSource(text.ToString(), known, project.CurrentTeam, project.SprintWeeks));
    }

    private static ArtifactVersion? Approved(Project project, ArtifactStage stage) =>
        project.Artifacts.Where(a => a.Stage == stage && a.Status == ArtifactStatus.Approved).OrderByDescending(a => a.Version).FirstOrDefault();

    private static void AppendApproved(Project project, ArtifactStage stage, string heading, StringBuilder text, HashSet<string> known)
    {
        var version = Approved(project, stage);
        if (version is null)
        {
            return;
        }

        text.AppendLine().AppendLine(heading);
        foreach (var item in StageMapper.ReadContent(version).Sections.SelectMany(s => s.Items))
        {
            Line(text, known, item.Id, item.Title, item.Text, item.Refs);
        }
    }

    private static void Line(StringBuilder text, HashSet<string> known, string id, string title, string detail, IReadOnlyList<string> covers)
    {
        known.Add(id);
        text.Append("- ").Append(id).Append(": ").Append(title).Append(" - ").Append(detail.ReplaceLineEndings(" "));
        if (covers.Count > 0)
        {
            text.Append(" (covers: ").Append(string.Join(", ", covers)).Append(')');
        }

        text.AppendLine();
    }
}
