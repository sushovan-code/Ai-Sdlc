using AiSdlc.Application.Stages.Content;

namespace AiSdlc.Application.Stages;

/// <summary>Checks and tidies stage content, whether it came from the model or from a person editing it.</summary>
public static class StageContentRules
{
    public const int MaxSections = 10;
    public const int MaxItemsPerSection = 100;
    public const int MaxSummaryLength = 2_000;
    public const int MaxTitleLength = 200;
    public const int MaxTextLength = 4_000;
    public const int MaxTags = 10;
    public const int MaxTagLength = 80;
    public const int MaxRefs = 30;

    /// <summary>Trims text and drops blank tags and refs.</summary>
    public static StageContent Normalize(StageContent content) => new(
        content.Summary.Trim(),
        content.Sections.Select(section => new StageSection(
            section.Title.Trim(),
            section.Items.Select(item => new StageItem(
                item.Id.Trim(),
                item.Title.Trim(),
                item.Text.Trim(),
                Clean(item.Tags),
                Clean(item.Refs))).ToList())).ToList());

    /// <returns>What is wrong with the content, or <c>null</c> when it is usable.</returns>
    public static string? Problem(StageContent content)
    {
        if (content.Summary.Length == 0 || content.Summary.Length > MaxSummaryLength)
        {
            return "The summary is missing or too long.";
        }

        if (content.Sections.Count is 0 or > MaxSections)
        {
            return "There must be between 1 and 10 sections.";
        }

        var items = content.Sections.SelectMany(s => s.Items).ToList();
        if (items.Count == 0)
        {
            return "There are no items.";
        }

        if (content.Sections.Any(s => s.Title.Length == 0 || s.Items.Count > MaxItemsPerSection))
        {
            return "Every section needs a title and at most 100 items.";
        }

        if (items.Any(i => i.Id.Length == 0 || i.Title.Length == 0 || i.Title.Length > MaxTitleLength || i.Text.Length > MaxTextLength))
        {
            return "Every item needs an id and a title, and titles and text must not be too long.";
        }

        if (items.Select(i => i.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() != items.Count)
        {
            return "Item ids must be unique.";
        }

        return items.Any(i => i.Tags.Count > MaxTags || i.Tags.Any(t => t.Length > MaxTagLength) || i.Refs.Count > MaxRefs)
            ? "An item has too many or too long tags or references."
            : null;
    }

    private static List<string> Clean(IReadOnlyList<string> values) =>
        values.Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v.Trim()).ToList();
}
