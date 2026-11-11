using System.Text.Json;
using AiSdlc.Application.Analysis.Agents;
using AiSdlc.Application.Stages.Content;

namespace AiSdlc.Application.Stages.Agents;

/// <summary>Reads what a model returned for a stage and checks it is usable.</summary>
public static class StageJson
{
    private const string DefaultPriority = "Medium";
    private const decimal DefaultHours = 8;
    private static readonly string[] Priorities = ["High", "Medium", "Low"];

    public static (StageContent? Content, string? Problem) ParseSections(string modelText)
    {
        try
        {
            var raw = JsonSerializer.Deserialize<SectionsShape>(AnalysisJson.StripFences(modelText), AnalysisJson.Options);
            if (raw is null)
            {
                return (null, "The answer was empty.");
            }

            var content = StageContentRules.Normalize(new StageContent(
                raw.Summary ?? string.Empty,
                (raw.Sections ?? []).Select(s => new StageSection(
                    s.Title ?? string.Empty,
                    (s.Items ?? []).Select(i => new StageItem(
                        i.Id ?? string.Empty,
                        i.Title ?? string.Empty,
                        i.Text ?? string.Empty,
                        i.Tags ?? [],
                        i.Refs ?? [])).ToList())).ToList()));
            var problem = StageContentRules.Problem(content);
            return problem is null ? (content, null) : (null, problem);
        }
        catch (JsonException exception)
        {
            return (null, $"The answer was not valid JSON ({exception.Message}).");
        }
    }

    public static (string? Summary, IReadOnlyList<TaskDraft>? Tasks, string? Problem) ParseTasks(string modelText)
    {
        try
        {
            var raw = JsonSerializer.Deserialize<TasksShape>(AnalysisJson.StripFences(modelText), AnalysisJson.Options);
            var summary = raw?.Summary?.Trim() ?? string.Empty;
            var tasks = (raw?.Tasks ?? []).Take(StageContentRules.MaxItemsPerSection).Select(ToDraft).ToList();

            if (summary.Length == 0 || summary.Length > StageContentRules.MaxSummaryLength)
            {
                return (null, null, "The summary is missing or too long.");
            }

            if (tasks.Count == 0)
            {
                return (null, null, "There are no tasks.");
            }

            if (tasks.Any(t => t.Id.Length == 0 || t.Title.Length == 0 || t.Title.Length > StageContentRules.MaxTitleLength))
            {
                return (null, null, "Every task needs an id and a title.");
            }

            return tasks.Select(t => t.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() != tasks.Count
                ? (null, null, "Task ids must be unique.")
                : (summary, tasks, null);
        }
        catch (JsonException exception)
        {
            return (null, null, $"The answer was not valid JSON ({exception.Message}).");
        }
    }

    private static TaskDraft ToDraft(TaskShape t) => new(
        (t.Id ?? string.Empty).Trim(),
        (t.Title ?? string.Empty).Trim(),
        (t.Description ?? string.Empty).Trim(),
        t.Hours is > 0 ? t.Hours.Value : DefaultHours,
        Priorities.FirstOrDefault(p => p.Equals(t.Priority?.Trim(), StringComparison.OrdinalIgnoreCase)) ?? DefaultPriority,
        Clean(t.Skills),
        Clean(t.DependsOn),
        Clean(t.Refs));

    private static List<string> Clean(List<string>? values) =>
        (values ?? []).Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v.Trim()).ToList();

    private sealed class SectionsShape
    {
        public string? Summary { get; set; }

        public List<SectionShape>? Sections { get; set; }
    }

    private sealed class SectionShape
    {
        public string? Title { get; set; }

        public List<ItemShape>? Items { get; set; }
    }

    private sealed class ItemShape
    {
        public string? Id { get; set; }

        public string? Title { get; set; }

        public string? Text { get; set; }

        public List<string>? Tags { get; set; }

        public List<string>? Refs { get; set; }
    }

    private sealed class TasksShape
    {
        public string? Summary { get; set; }

        public List<TaskShape>? Tasks { get; set; }
    }

    private sealed class TaskShape
    {
        public string? Id { get; set; }

        public string? Title { get; set; }

        public string? Description { get; set; }

        public decimal? Hours { get; set; }

        public string? Priority { get; set; }

        public List<string>? Skills { get; set; }

        public List<string>? DependsOn { get; set; }

        public List<string>? Refs { get; set; }
    }
}
