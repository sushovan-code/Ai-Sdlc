using System.Text.Json;
using AiSdlc.Application.Analysis.Content;

namespace AiSdlc.Application.Analysis.Agents;

/// <summary>Reads and writes the analysis JSON, and checks that what a model returned is usable.</summary>
public static class AnalysisJson
{
    public const int MaxItemsPerList = 100;
    public const int MaxSummaryLength = 2000;
    public const int MaxTitleLength = 200;
    public const int MaxTextLength = 2000;

    private const string DefaultLevel = "Medium";
    private static readonly string[] Levels = ["High", "Medium", "Low"];

    public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
    };

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);

    public static T? Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, Options);

    /// <summary>Parses a model answer into an analysis. Returns the problem when it cannot be used.</summary>
    public static (AnalysisContent? Content, string? Problem) ParseDraft(string modelText)
    {
        try
        {
            var raw = JsonSerializer.Deserialize<DraftShape>(StripFences(modelText), Options);
            return raw is null ? (null, "The answer was empty.") : Validate(Normalize(raw));
        }
        catch (JsonException exception)
        {
            return (null, $"The answer was not valid JSON ({exception.Message}).");
        }
    }

    public static IReadOnlyList<CriticNote> ParseCriticNotes(string modelText)
    {
        var shape = JsonSerializer.Deserialize<CriticShape>(StripFences(modelText), Options);
        return (shape?.Notes ?? [])
            .Where(n => !string.IsNullOrWhiteSpace(n.Note))
            .Take(8)
            .Select(n => new CriticNote(Level(n.Severity), n.Note.Trim()))
            .ToList();
    }

    private static AnalysisContent Normalize(DraftShape raw) => new(
        (raw.Summary ?? string.Empty).Trim(),
        Requirements(raw.FunctionalRequirements),
        Requirements(raw.NonFunctionalRequirements),
        Questions(raw.Ambiguities),
        Questions(raw.Gaps),
        (raw.Assumptions ?? []).Where(a => !string.IsNullOrWhiteSpace(a)).Select(a => a.Trim()).Take(MaxItemsPerList).ToList(),
        (raw.Risks ?? []).Take(MaxItemsPerList)
            .Select(r => new RiskItem(Clean(r.Id), Clean(r.Description), Level(r.Severity), Refs(r.SourceRefs))).ToList(),
        []);

    private static (AnalysisContent? Content, string? Problem) Validate(AnalysisContent content)
    {
        if (content.Summary.Length == 0 || content.Summary.Length > MaxSummaryLength)
        {
            return (null, "The summary is missing or too long.");
        }

        if (content.FunctionalRequirements.Count == 0)
        {
            return (null, "There are no functional requirements.");
        }

        var ids = content.FunctionalRequirements.Concat(content.NonFunctionalRequirements).Select(r => r.Id).ToList();
        if (ids.Any(string.IsNullOrWhiteSpace) || ids.Distinct(StringComparer.OrdinalIgnoreCase).Count() != ids.Count)
        {
            return (null, "Requirement ids must be present and unique.");
        }

        var tooLong = content.FunctionalRequirements.Concat(content.NonFunctionalRequirements)
            .Any(r => r.Title.Length > MaxTitleLength || r.Description.Length > MaxTextLength);
        return tooLong ? (null, "A requirement title or description is too long.") : (content, null);
    }

    private static List<RequirementItem> Requirements(List<RequirementShape>? items) =>
        (items ?? []).Take(MaxItemsPerList)
            .Select(r => new RequirementItem(Clean(r.Id), Clean(r.Title), Clean(r.Description), Level(r.Priority), Refs(r.SourceRefs)))
            .ToList();

    private static List<QuestionItem> Questions(List<QuestionShape>? items) =>
        (items ?? []).Take(MaxItemsPerList)
            .Where(q => !string.IsNullOrWhiteSpace(q.Text))
            .Select(q => new QuestionItem(Clean(q.Id), Clean(q.Text), Refs(q.SourceRefs)))
            .ToList();

    private static string Clean(string? value) => (value ?? string.Empty).Trim();

    private static string Level(string? value) =>
        Levels.FirstOrDefault(l => l.Equals(value?.Trim(), StringComparison.OrdinalIgnoreCase)) ?? DefaultLevel;

    private static List<string> Refs(List<string>? refs) =>
        (refs ?? []).Where(r => !string.IsNullOrWhiteSpace(r)).Select(r => r.Trim()).ToList();

    /// <summary>Models sometimes wrap JSON in a markdown code fence even when told not to.</summary>
    public static string StripFences(string text)
    {
        var trimmed = text.Trim();
        if (!trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            return trimmed;
        }

        var firstLineEnd = trimmed.IndexOf('\n', StringComparison.Ordinal);
        var lastFence = trimmed.LastIndexOf("```", StringComparison.Ordinal);
        return firstLineEnd >= 0 && lastFence > firstLineEnd ? trimmed[(firstLineEnd + 1)..lastFence].Trim() : trimmed;
    }

    private sealed class DraftShape
    {
        public string? Summary { get; set; }

        public List<RequirementShape>? FunctionalRequirements { get; set; }

        public List<RequirementShape>? NonFunctionalRequirements { get; set; }

        public List<QuestionShape>? Ambiguities { get; set; }

        public List<QuestionShape>? Gaps { get; set; }

        public List<string>? Assumptions { get; set; }

        public List<RiskShape>? Risks { get; set; }
    }

    private sealed class RequirementShape
    {
        public string? Id { get; set; }

        public string? Title { get; set; }

        public string? Description { get; set; }

        public string? Priority { get; set; }

        public List<string>? SourceRefs { get; set; }
    }

    private sealed class QuestionShape
    {
        public string? Id { get; set; }

        public string? Text { get; set; }

        public List<string>? SourceRefs { get; set; }
    }

    private sealed class RiskShape
    {
        public string? Id { get; set; }

        public string? Description { get; set; }

        public string? Severity { get; set; }

        public List<string>? SourceRefs { get; set; }
    }

    private sealed class CriticShape
    {
        public List<NoteShape>? Notes { get; set; }
    }

    private sealed class NoteShape
    {
        public string? Severity { get; set; }

        public string Note { get; set; } = string.Empty;
    }
}
