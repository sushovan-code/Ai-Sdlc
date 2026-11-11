using AiSdlc.Application.Analysis.Content;

namespace AiSdlc.Application.Analysis.Agents;

/// <summary>
/// The retrieval agent. For every item it checks the cited chunk ids against the real document, drops citations that do
/// not exist, and attaches the quoted text as evidence. Items left without any valid citation are reported so a
/// reviewer sees which claims the document does not back up.
/// </summary>
public static class EvidenceRetriever
{
    public const int MaxQuoteCharacters = 280;

    public static EvidenceResult Attach(AnalysisContent draft, IReadOnlyList<SourceChunk> chunks)
    {
        var known = chunks.ToDictionary(c => c.Id, StringComparer.OrdinalIgnoreCase);
        var unsupported = new List<string>();
        var cited = new SortedSet<string>(ChunkOrder.Instance);

        IReadOnlyList<string> Valid(string id, IReadOnlyList<string> refs, bool requiresEvidence)
        {
            var valid = refs.Select(r => r.Trim().ToUpperInvariant()).Where(known.ContainsKey).Distinct().ToList();
            valid.ForEach(r => cited.Add(r));
            if (valid.Count == 0 && requiresEvidence)
            {
                unsupported.Add(id);
            }

            return valid;
        }

        var content = draft with
        {
            FunctionalRequirements = draft.FunctionalRequirements.Select(r => r with { SourceRefs = Valid(r.Id, r.SourceRefs, true) }).ToList(),
            NonFunctionalRequirements = draft.NonFunctionalRequirements.Select(r => r with { SourceRefs = Valid(r.Id, r.SourceRefs, true) }).ToList(),
            Ambiguities = draft.Ambiguities.Select(q => q with { SourceRefs = Valid(q.Id, q.SourceRefs, false) }).ToList(),
            Gaps = draft.Gaps.Select(q => q with { SourceRefs = Valid(q.Id, q.SourceRefs, false) }).ToList(),
            Risks = draft.Risks.Select(r => r with { SourceRefs = Valid(r.Id, r.SourceRefs, false) }).ToList(),
        };

        var evidence = cited.Select(id => new Evidence(id, Quote(known[id].Text))).ToList();
        return new EvidenceResult(content with { Evidence = evidence }, unsupported);
    }

    private static string Quote(string text) =>
        text.Length <= MaxQuoteCharacters ? text : text[..MaxQuoteCharacters].TrimEnd() + "…";

    /// <summary>Orders C2 before C10.</summary>
    private sealed class ChunkOrder : IComparer<string>
    {
        public static ChunkOrder Instance { get; } = new();

        public int Compare(string? x, string? y) => Number(x).CompareTo(Number(y));

        private static int Number(string? id) =>
            int.TryParse(id?.TrimStart('C', 'c'), out var n) ? n : int.MaxValue;
    }
}
