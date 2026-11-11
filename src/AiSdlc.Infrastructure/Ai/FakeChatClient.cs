using System.Text.RegularExpressions;
using AiSdlc.Application.Analysis.Agents;
using Microsoft.Extensions.AI;

namespace AiSdlc.Infrastructure.Ai;

/// <summary>
/// An offline <see cref="IChatClient"/> that answers the analysis prompts with a simple, deterministic draft built from
/// the document chunks. It lets the whole flow run and be tested without a key or a network.
/// </summary>
internal sealed partial class FakeChatClient : IChatClient
{
    public const string ModelName = "fake-analysis-1";
    private const int MaxRequirements = 6;
    private const int TitleLength = 60;
    private const int DescriptionLength = 240;
    private const string CriticMarker = "Review agent";
    private static readonly string[] QualityWords = ["secur", "perform", "availab", "audit", "privacy", "complian", "encrypt"];

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var all = messages.ToList();
        var isCritic = all.Any(m => m.Role == ChatRole.System && m.Text.Contains(CriticMarker, StringComparison.Ordinal));
        var user = all.Last(m => m.Role == ChatRole.User).Text;
        var json = isCritic ? CriticJson() : SpecialistJson(user);
        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, json)) { ModelId = ModelName });
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("The offline client does not stream.");

    public object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceType == typeof(ChatClientMetadata) ? new ChatClientMetadata("fake", null, ModelName) : null;

    public void Dispose()
    {
    }

    private static string CriticJson() =>
        AnalysisJson.Serialize(new { notes = new[] { new { severity = "Low", note = "Offline review: check the draft against the source before approving." } } });

    private static string SpecialistJson(string prompt)
    {
        var chunks = ChunkLine().Matches(Document(prompt)).Select(m => (Id: m.Groups[1].Value, Text: m.Groups[2].Value.Trim())).ToList();
        var functional = chunks.Take(MaxRequirements).Select((c, i) => Requirement($"FR-{i + 1}", c)).ToList();
        var quality = chunks.Where(c => QualityWords.Any(w => c.Text.Contains(w, StringComparison.OrdinalIgnoreCase))).Take(3)
            .Select((c, i) => Requirement($"NFR-{i + 1}", c)).ToList();

        var assumptions = new List<string> { "Generated offline by the built-in test model, not a real language model." };
        var feedback = Feedback().Match(prompt);
        if (feedback.Success)
        {
            assumptions.Add($"Reviewer feedback considered: {feedback.Groups[1].Value.Trim()}");
        }

        return AnalysisJson.Serialize(new
        {
            summary = $"The document describes {chunks.Count} section(s) of required behaviour.",
            functionalRequirements = functional,
            nonFunctionalRequirements = quality,
            ambiguities = chunks.Count == 0 ? [] : new[] { new { id = "AMB-1", text = "Which users and roles are in scope?", sourceRefs = new[] { chunks[0].Id } } },
            gaps = new[] { new { id = "GAP-1", text = "Acceptance criteria are not stated.", sourceRefs = Array.Empty<string>() } },
            assumptions,
            risks = Array.Empty<object>(),
        });
    }

    private static object Requirement(string id, (string Id, string Text) chunk) => new
    {
        id,
        title = Truncate(chunk.Text, TitleLength),
        description = Truncate(chunk.Text, DescriptionLength),
        priority = "Medium",
        sourceRefs = new[] { chunk.Id },
    };

    private static string Document(string prompt)
    {
        var start = prompt.IndexOf("<document>", StringComparison.Ordinal);
        var end = prompt.IndexOf("</document>", StringComparison.Ordinal);
        return start >= 0 && end > start ? prompt[start..end] : string.Empty;
    }

    private static string Truncate(string text, int length) => text.Length <= length ? text : text[..length].TrimEnd() + "...";

    [GeneratedRegex(@"^\[(C\d+)\] (.*)$", RegexOptions.Multiline)]
    private static partial Regex ChunkLine();

    [GeneratedRegex(@"<reviewer_feedback>\s*(.*?)\s*</reviewer_feedback>", RegexOptions.Singleline)]
    private static partial Regex Feedback();
}
