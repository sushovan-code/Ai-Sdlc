using AiSdlc.Application.Analysis.Content;
using AiSdlc.Application.Common;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace AiSdlc.Application.Analysis.Agents;

/// <summary>
/// The specialist agent: asks the model for a structured requirements analysis. It caps the output size, checks the
/// answer against the schema, and retries once with the problem described before giving up.
/// </summary>
public sealed partial class AnalysisSpecialist(IChatClient chat, ILogger<AnalysisSpecialist> logger)
{
    private const int MaxAttempts = 2;

    public async Task<Result<SpecialistOutput>> DraftAsync(
        IReadOnlyList<SourceChunk> chunks, AnalysisContent? previous, string? feedback, CancellationToken cancellationToken)
    {
        var previousJson = previous is null ? null : AnalysisJson.Serialize(previous with { Evidence = [] });
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, AnalysisPrompts.SpecialistSystem),
            new(ChatRole.User, AnalysisPrompts.SpecialistUser(chunks, previousJson, feedback)),
        };
        var options = new ChatOptions { MaxOutputTokens = AiLimits.MaxOutputTokens, ResponseFormat = ChatResponseFormat.Json };

        string? problem = null;
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            ChatResponse response;
            try
            {
                response = await chat.GetResponseAsync(messages, options, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                LogCallFailed(exception);
                return Result<SpecialistOutput>.Failure(AnalysisErrors.AiUnavailable);
            }

            var parsed = AnalysisJson.ParseDraft(response.Text);
            if (parsed.Content is not null)
            {
                return Result<SpecialistOutput>.Success(new SpecialistOutput(
                    parsed.Content,
                    response.ModelId ?? chat.GetService<ChatClientMetadata>()?.DefaultModelId ?? "unknown",
                    response.Usage?.TotalTokenCount));
            }

            problem = parsed.Problem;
            messages.Add(new ChatMessage(ChatRole.Assistant, response.Text));
            messages.Add(new ChatMessage(ChatRole.User, AnalysisPrompts.RetryUser(problem!)));
        }

        LogInvalidOutput(problem);
        return Result<SpecialistOutput>.Failure(AnalysisErrors.AiInvalidOutput);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "The AI model call for the analysis failed.")]
    private partial void LogCallFailed(Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The AI model returned unusable analysis output twice: {Problem}")]
    private partial void LogInvalidOutput(string? problem);
}
