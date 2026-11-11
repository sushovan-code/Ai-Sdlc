using AiSdlc.Application.Analysis.Content;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace AiSdlc.Application.Analysis.Agents;

/// <summary>
/// The review agent: reads the draft against the document and lists gaps, inconsistencies and risks for the human
/// reviewer. Its notes are advice, so if it fails the analysis still goes ahead, with a note saying the review is missing.
/// </summary>
public sealed partial class AnalysisCritic(IChatClient chat, ILogger<AnalysisCritic> logger)
{
    public const string UnavailableNote = "The automatic review of this draft could not be completed. Please read it carefully.";

    public async Task<IReadOnlyList<CriticNote>> ReviewAsync(
        IReadOnlyList<SourceChunk> chunks, AnalysisContent draft, CancellationToken cancellationToken)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, AnalysisPrompts.CriticSystem),
            new(ChatRole.User, AnalysisPrompts.CriticUser(chunks, AnalysisJson.Serialize(draft with { Evidence = [] }))),
        };
        var options = new ChatOptions { MaxOutputTokens = AiLimits.CriticMaxOutputTokens, ResponseFormat = ChatResponseFormat.Json };

        try
        {
            var response = await chat.GetResponseAsync(messages, options, cancellationToken);
            return AnalysisJson.ParseCriticNotes(response.Text);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogReviewFailed(exception);
            return [new CriticNote("Low", UnavailableNote)];
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "The critic review of the analysis failed; continuing without it.")]
    private partial void LogReviewFailed(Exception exception);
}
