using System.Text;
using AiSdlc.Application.Analysis.Content;

namespace AiSdlc.Application.Analysis.Agents;

/// <summary>
/// Splits the requirements text into numbered chunks (C1, C2, ...) on paragraph boundaries. The analysis cites chunk
/// ids, which is how every claim can be traced back to the document and checked.
/// </summary>
public static class RequirementsChunker
{
    public const int TargetCharacters = 1200;

    public static IReadOnlyList<SourceChunk> Split(string text)
    {
        var chunks = new List<SourceChunk>();
        var current = new StringBuilder();

        void Flush()
        {
            if (current.Length > 0)
            {
                chunks.Add(new SourceChunk($"C{chunks.Count + 1}", current.ToString().Trim()));
                current.Clear();
            }
        }

        foreach (var paragraph in Paragraphs(text))
        {
            foreach (var piece in Pieces(paragraph))
            {
                if (current.Length > 0 && current.Length + piece.Length + 1 > TargetCharacters)
                {
                    Flush();
                }

                current.Append(current.Length > 0 ? "\n" : string.Empty).Append(piece);
            }
        }

        Flush();
        return chunks;
    }

    private static string[] Paragraphs(string text) =>
        text.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>A very long paragraph is cut at spaces so no single chunk is huge.</summary>
    private static IEnumerable<string> Pieces(string paragraph)
    {
        var remaining = paragraph;
        while (remaining.Length > TargetCharacters)
        {
            var cut = remaining.LastIndexOf(' ', TargetCharacters);
            cut = cut > 0 ? cut : TargetCharacters;
            yield return remaining[..cut].Trim();
            remaining = remaining[cut..].Trim();
        }

        if (remaining.Length > 0)
        {
            yield return remaining;
        }
    }
}
