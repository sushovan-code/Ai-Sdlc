using System.Globalization;
using System.Text;
using AiSdlc.Application.Analysis.Agents;
using AiSdlc.Application.Analysis.Content;
using AiSdlc.Application.Stages;
using AiSdlc.Domain.Artifacts;
using AiSdlc.Domain.Projects;

namespace AiSdlc.Application.Wrapup.GetReport;

/// <summary>Writes the project report as Markdown: each stage's current output, who reviewed it and the traceability gaps.</summary>
public static class ReportBuilder
{
    private static readonly (ArtifactStage Stage, string Title)[] Stages =
    [
        (ArtifactStage.Analysis, "Requirements analysis"),
        (ArtifactStage.Architecture, "Architecture plan"),
        (ArtifactStage.Tasks, "Development tasks"),
        (ArtifactStage.TestCases, "QA test cases"),
    ];

    public static string Build(Project project, DateTimeOffset now)
    {
        var report = new StringBuilder();
        report.Append("# ").AppendLine(project.Name);
        if (!string.IsNullOrWhiteSpace(project.Description))
        {
            report.AppendLine().AppendLine(project.Description);
        }

        report.AppendLine().AppendLine(CultureInfo.InvariantCulture, $"Stage: {project.Stage}. Sprint length: {project.SprintWeeks} weeks. Report created {now:yyyy-MM-dd HH:mm} UTC.");

        foreach (var (stage, title) in Stages)
        {
            report.AppendLine().Append("## ").AppendLine(title);
            var version = Current(project, stage);
            if (version is null)
            {
                report.AppendLine().AppendLine("Not drafted yet.");
                continue;
            }

            report.AppendLine().AppendLine(Status(version));
            if (stage == ArtifactStage.Analysis)
            {
                Analysis(report, version);
            }
            else
            {
                Stage(report, version);
            }

            Decisions(report, version);
        }

        return report.ToString();
    }

    private static ArtifactVersion? Current(Project project, ArtifactStage stage) =>
        project.Artifacts
            .Where(a => a.Stage == stage && a.Status is ArtifactStatus.PendingReview or ArtifactStatus.Approved)
            .OrderByDescending(a => a.Version)
            .FirstOrDefault();

    private static string Status(ArtifactVersion version) =>
        $"Version {version.Version}, {(version.Status == ArtifactStatus.Approved ? "approved" : "waiting for review (not approved)")}, " +
        $"{(version.Source == ArtifactSource.Human ? "edited by hand" : $"drafted by {version.Model}")}.";

    private static void Analysis(StringBuilder report, ArtifactVersion version)
    {
        var content = AnalysisJson.Deserialize<AnalysisContent>(version.ContentJson);
        if (content is null)
        {
            return;
        }

        report.AppendLine().AppendLine(content.Summary);
        Requirements(report, "Functional requirements", content.FunctionalRequirements);
        Requirements(report, "Non-functional requirements", content.NonFunctionalRequirements);
        Bullets(report, "Questions to settle", content.Ambiguities.Select(a => $"{a.Id}: {a.Text}").Concat(content.Gaps.Select(g => $"{g.Id}: {g.Text}")));
        Bullets(report, "Assumptions", content.Assumptions);
        Bullets(report, "Risks", content.Risks.Select(r => $"{r.Id}: {r.Description} ({r.Severity})"));
    }

    private static void Requirements(StringBuilder report, string heading, IReadOnlyList<RequirementItem> items) =>
        Bullets(report, heading, items.Select(r => $"**{r.Id} {r.Title}** ({r.Priority}): {r.Description}"));

    private static void Stage(StringBuilder report, ArtifactVersion version)
    {
        var content = StageMapper.ReadContent(version);
        report.AppendLine().AppendLine(content.Summary);
        foreach (var section in content.Sections)
        {
            Bullets(report, section.Title, section.Items.Select(Describe));
        }
    }

    private static string Describe(Stages.Content.StageItem item)
    {
        var line = new StringBuilder($"**{item.Id} {item.Title}**");
        if (item.Tags.Count > 0)
        {
            line.Append(" [").Append(string.Join(", ", item.Tags)).Append(']');
        }

        if (item.Text.Length > 0)
        {
            line.Append(": ").Append(item.Text.ReplaceLineEndings(" "));
        }

        if (item.Refs.Count > 0)
        {
            line.Append(" (traces to ").Append(string.Join(", ", item.Refs)).Append(')');
        }

        return line.ToString();
    }

    private static void Decisions(StringBuilder report, ArtifactVersion version)
    {
        if (version.Decisions.Count == 0)
        {
            return;
        }

        Bullets(
            report,
            "Review",
            version.Decisions.OrderBy(d => d.DecidedAtUtc).Select(d =>
                $"{d.Kind} by {d.ReviewerRole} on {d.DecidedAtUtc:yyyy-MM-dd HH:mm} UTC{(string.IsNullOrWhiteSpace(d.Comment) ? string.Empty : $": {d.Comment}")}"));
    }

    private static void Bullets(StringBuilder report, string heading, IEnumerable<string> lines)
    {
        var list = lines.ToList();
        if (list.Count == 0)
        {
            return;
        }

        report.AppendLine().Append("### ").AppendLine(heading).AppendLine();
        foreach (var line in list)
        {
            report.Append("- ").AppendLine(line);
        }
    }
}
