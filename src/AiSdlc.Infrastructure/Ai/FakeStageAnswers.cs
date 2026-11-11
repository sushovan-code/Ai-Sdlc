using System.Text.RegularExpressions;
using AiSdlc.Application.Analysis.Agents;

namespace AiSdlc.Infrastructure.Ai;

/// <summary>Deterministic offline answers for the architecture, task and test-case prompts, built from the input lines.</summary>
internal static partial class FakeStageAnswers
{
    public const string ArchitectureMarker = "Architecture agent";
    public const string TasksMarker = "Tasks agent";
    public const string TestCasesMarker = "Test case agent";

    private const int MaxItems = 6;
    private const int TitleLength = 60;
    private const string DefaultSkill = "C#";
    private static readonly string[] Priorities = ["High", "Medium", "Low"];

    private sealed record Line(string Id, string Title, string Detail, string[] Covers);

    public static bool IsStagePrompt(string system) =>
        system.Contains(ArchitectureMarker, StringComparison.Ordinal)
        || system.Contains(TasksMarker, StringComparison.Ordinal)
        || system.Contains(TestCasesMarker, StringComparison.Ordinal);

    public static string Answer(string system, string user)
    {
        var lines = InputLines().Matches(user).Select(ToLine).ToList();
        if (system.Contains(TasksMarker, StringComparison.Ordinal))
        {
            return Tasks(lines, Skills(user));
        }

        return system.Contains(ArchitectureMarker, StringComparison.Ordinal) ? Architecture(lines) : TestCases(lines);
    }

    private static string Architecture(List<Line> lines)
    {
        var requirements = lines.Where(l => l.Id.Contains("FR-", StringComparison.Ordinal) || l.Id.StartsWith("NFR-", StringComparison.Ordinal)).ToList();
        var first = requirements.Select(r => r.Id).Take(1).ToArray();
        var components = requirements.Take(3).Select((r, i) => Item($"C-{i + 1}", $"{Short(r.Title)} service", r.Detail, ["Offline stack"], [r.Id]));

        return AnalysisJson.Serialize(new
        {
            summary = "A layered web application with one service per capability, a shared database and an API in front.",
            sections = new object[]
            {
                new { title = "Components", items = components.ToArray() },
                new { title = "Data and integration", items = new[] { Item("D-1", "Relational database", "Stores projects, users and audit records.", ["SQL"], first) } },
                new { title = "Key decisions", items = new[] { Item("K-1", "Layered architecture", "Keeps domain rules apart from infrastructure so adapters can be swapped.", [], first) } },
                new { title = "Risks", items = new[] { Item("R-1", "Unclear scale needs", "Load figures are not stated, so capacity is a guess.", [], first) } },
            },
        });
    }

    private static string Tasks(List<Line> lines, string[] skills)
    {
        var basis = lines.Where(l => l.Id.StartsWith("C-", StringComparison.Ordinal)).ToList();
        if (basis.Count == 0)
        {
            basis = lines.Where(l => l.Id.Contains("FR-", StringComparison.Ordinal)).ToList();
        }

        var tasks = basis.Take(MaxItems).Select((l, i) => new
        {
            id = $"T-{i + 1}",
            title = $"Build {Short(l.Title)}",
            description = l.Detail,
            hours = i % 3 == 2 ? 16 : 8,
            priority = Priorities[i % Priorities.Length],
            skills = new[] { skills.Length == 0 ? DefaultSkill : skills[i % skills.Length] },
            dependsOn = i > 0 && i % 3 == 0 ? new[] { $"T-{i}" } : [],
            refs = l.Covers.Append(l.Id).Distinct().ToArray(),
        }).ToArray();

        return AnalysisJson.Serialize(new { summary = $"{tasks.Length} tasks cover the approved architecture.", tasks });
    }

    private static string TestCases(List<Line> lines)
    {
        var tasks = lines.Where(l => l.Id.StartsWith("T-", StringComparison.Ordinal)).Take(MaxItems).ToList();
        var cases = tasks.Select((t, i) => Item(
            $"TC-{i + 1}",
            $"Verify: {Short(t.Title)}",
            $"1. Set up the scenario.\n2. Run the feature.\nExpected: {Short(t.Detail)} works as described.",
            ["Functional"],
            t.Covers.Append(t.Id).Distinct().ToArray())).ToArray();
        var requirements = lines.Where(l => l.Id.Contains("FR-", StringComparison.Ordinal)).Select(l => l.Id).Take(1).ToArray();

        return AnalysisJson.Serialize(new
        {
            summary = $"{cases.Length} functional cases trace to the approved tasks.",
            sections = new object[]
            {
                new { title = "Functional tests", items = cases },
                new { title = "Non-functional tests", items = new[] { Item("NT-1", "Response time", "1. Load the main page.\nExpected: it answers within three seconds.", ["Performance"], requirements) } },
            },
        });
    }

    private static object Item(string id, string title, string text, string[] tags, string[] refs) =>
        new { id, title, text, tags, refs };

    private static Line ToLine(Match m) => new(
        m.Groups[1].Value,
        m.Groups[2].Value.Trim(),
        m.Groups[3].Success ? m.Groups[3].Value.Trim() : m.Groups[2].Value.Trim(),
        m.Groups[4].Success ? m.Groups[4].Value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries) : []);

    private static string[] Skills(string user)
    {
        var match = SkillsLine().Match(user);
        return match.Success
            ? match.Groups[1].Value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            : [];
    }

    private static string Short(string text) => text.Length <= TitleLength ? text : text[..TitleLength].TrimEnd() + "...";

    [GeneratedRegex(@"^- ([A-Za-z]+-\d+): (.*?)(?: - (.*?))?(?: \(covers: (.*)\))?$", RegexOptions.Multiline)]
    private static partial Regex InputLines();

    [GeneratedRegex(@"^Team skills: (.*)$", RegexOptions.Multiline)]
    private static partial Regex SkillsLine();
}
